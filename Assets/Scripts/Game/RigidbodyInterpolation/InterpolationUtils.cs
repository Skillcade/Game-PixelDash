using System;
using System.Collections.Generic;
using FishNet;
using UnityEngine;

namespace Game.RigidbodyInterpolation
{
    public static class InterpolationUtils
    {
        // Интервал отправки сообщений. Фотон его не использует, у него свои вычисления.
        // Фотон предоставляет нам частоту отправки - по ней можно вычислить интервал
        public static float SendInterval => 1f / InstanceFinder.TimeManager.TickRate;
        public static int SendRate => InstanceFinder.TimeManager.TickRate;

        // Вычисляем фактическое время отставания (буферное время) в зависимости от того, как приходят снапшоты
        // Т.е. мы запланировали, что будем отставать на интервал отправки 1 снапшота, а по факту отстаём на 1.1 или 0.9 от этого интервала
        // Здесь вычисляется множитель этого отставания
        public static float DynamicAdjustment(
            float sendInterval, // Интервал между отправкой снапшотов
            float jitterStandardDeviation, // Насколько интервал получения снапшотов в среднем отличается от запланированного интервала отправки 
            float dynamicAdjustmentTolerance) // Насколько большое отклонение интервала получения от интервала отправки мы можем допустить
        {
            float intervalWithJitter = sendInterval + jitterStandardDeviation;
            float multiples = intervalWithJitter / sendInterval;

            float safeZone = multiples + dynamicAdjustmentTolerance;
            return safeZone;
        }

        // Буфер сам сортирует снапшоты по их времени отправки (RemoteTime).
        // SortedList это что-то типа словаря, который отсортирован по ключу
        // Записываем новый снапшот в буфер с его временем отправки - если ВДРУГ такое время отправки уже есть, то просто обновляем данные
        // Если количество снапшотов в буфере превышает лимит, все новые снапшоты игнорируются (возможно, зря - может, стоит выбрасывать самые старые? Надо протестить)
        // Возвращаем true, если снапшот добавлен в буфер (при добавлении он сортирует автоматически)
        // Возвращаем false, если буфер переполнен, или если уже был снапшот с таким RemoteTime, а новый его просто перезаписал
        public static bool InsertIfNotExists<T>(
            SortedList<float, T> buffer, // snapshot buffer
            int bufferLimit, // don't grow infinitely
            T snapshot) // the newly received snapshot
            where T : IInterpolateSnapshot
        {
            if (buffer.Count >= bufferLimit) return false;

            int before = buffer.Count;
            buffer[snapshot.RemoteTime] = snapshot; // overwrites if key exists
            return buffer.Count > before;
        }

        // Добавляем новый снапшот в список и подгоняем локальное время
        public static void InsertAndAdjust<T>(
            SortedList<float, T> buffer, // snapshot buffer
            SnapshotInterpolationSettings interpolationSettings,
            T snapshot, // the newly received snapshot
            ref float localTimeline, // local interpolation time based on server time
            ref float localTimescale, // timeline multiplier to apply catchup / slowdown over time
            float bufferTime, // offset for buffering
            ref ExponentalMovingAverage driftEma, // for catchup / slowdown
            ref ExponentalMovingAverage deliveryTimeEma) // for dynamic buffer time adjustment
            where T : IInterpolateSnapshot
        {
            // Если буфер пустой, то жёстко устанавливаем локальное время равным отставанию буфера, чтобы синхронизировать таймлайны
            if (buffer.Count == 0)
                localTimeline = snapshot.RemoteTime - bufferTime;

            if (!InsertIfNotExists(buffer, interpolationSettings.BufferLimit, snapshot))
                return;

            if (buffer.Count >= 2)
            {
                // Если снапшотов несколько, вычисляем последний интервал доставки
                // и добавляем его в вычисление среднего времени доставки

                // Среднее время вычисляется по формуле Exponental Moving Average - среднее из нескольких последних значений, где у последних больше веса

                // Больше ни для чего LocalTime не используется, он нужен только для подгонки отставания буфера,
                // на случай если вдруг снапшоты начнут приходить быстрее или медленнее

                float previousLocalTime = buffer.Values[buffer.Count - 2].LocalTime;
                float lastLocalTime = buffer.Values[buffer.Count - 1].LocalTime;
                float localDeliveryTime = lastLocalTime - previousLocalTime;

                deliveryTimeEma.Add(localDeliveryTime);
            }

            // Обрезаем локальное время под полученное RemoteTime, чтобы не сильно обгонять или отставать от хоста
            float latestRemoteTime = snapshot.RemoteTime;
            localTimeline = TimelineClamp(localTimeline, bufferTime, latestRemoteTime);

            // Вычисляем отставание локального времени и добавляем в вычисление среднего отставания
            float timeDiff = latestRemoteTime - localTimeline;
            driftEma.Add(timeDiff);

            // Вычисляем, насколько мы отстаём или опережаем нужное время
            // Нужное время это время отставания буфера
            // Наша цель, чтобы время отставания от RemoteTime было равно отставанию буфера
            // Подстраиваем timeScale под это отставание
            float drift = driftEma.Value - bufferTime;
            localTimescale = Timescale(drift, interpolationSettings);
        }

        // По буферу и текущему локальному времени вычисляем, по каким снапшотам мы сейчас интерполируемся и на какое значение
        // Удаляем все снапшоты, которые более не используются 
        public static void StepInterpolation<T>(
            SortedList<float, T> buffer, // snapshot buffer
            float localTimeline, // local interpolation time based on server time
            out T fromSnapshot, // we interpolate 'from' this snapshot
            out T toSnapshot, // 'to' this snapshot
            out float t) // at ratio 't' [0,1]
            where T : IInterpolateSnapshot
        {
            // check this in caller:
            // nothing to do if there are no snapshots at all yet

            // sample snapshot buffer at local interpolation time
            // Вычисляем индексы снапшотов, по которым интерполируем
            Sample(buffer, localTimeline, out int from, out int to, out t);

            // save from/to
            fromSnapshot = buffer.Values[from];
            toSnapshot = buffer.Values[to];

            // remove older snapshots that we definitely don't need anymore.
            // after(!) using the indices.
            //
            // if we have 3 snapshots, and we are between 2nd and 3rd:
            //   from = 1, to = 2
            // then we need to remove the first one, which is exactly 'from'.
            // because 'from-1' = 0 would remove none.
            // remember that buffer is sorted from lowest to highest, so older snapshots are in front,
            // so when we have 3 snapshots, we interpolate between latest two, and their indices are 1 and 2,
            // so we need to remove first snapshots,
            // so count to remove snapshots is equal to index of 'from'
            buffer.RemoveRange(from);
        }

        // Вычисляем, между какими двумя снапшотами мы сейчас интерполируем
        private static void Sample<T>(
            SortedList<float, T> buffer, // snapshot buffer
            float localTimeline, // local interpolation time based on server time
            out int from, // the snapshot <= time
            out int to, // the snapshot >= time
            out float t) // interpolation factor
            where T : IInterpolateSnapshot
        {
            from = -1;
            to = -1;
            t = 0;

            // sample from [0,count-1] so we always have two at 'i' and 'i+1'.
            for (int i = 0; i < buffer.Count - 1; i++)
            {
                // is local time between these two?
                var first = buffer.Values[i];
                var second = buffer.Values[i + 1];
                if (localTimeline < first.RemoteTime || localTimeline > second.RemoteTime)
                    continue;

                // use these two snapshots
                from = i;
                to = i + 1;
                t = Mathf.InverseLerp(first.RemoteTime, second.RemoteTime, localTimeline);
                return;
            }

            // oldest snapshot ahead of local time?
            if (buffer.Values[0].RemoteTime > localTimeline)
            {
                from = to = 0;
                t = 0;
            }
            // otherwise initialize both to the last one
            else
            {
                from = to = buffer.Count - 1;
                t = 0;
            }
        }

        // Смотрим, насколько локальное время отстаёт от вычисленного RemoteTime или обногяет его и вычисляем множитель локального времени
        // Если отклонение от RemoteTime в пределах погрешности, то множитель локального времени - 1, локальное время не ускоряется и не замедляется 
        private static float Timescale(float drift, SnapshotInterpolationSettings interpolationSettings)
        {
            if (drift > SendInterval * interpolationSettings.CatchupPositiveThreshold)
                return 1 + interpolationSettings.CatchupSpeed;

            if (drift < SendInterval * interpolationSettings.CatchupNegativeThreshold)
                return 1 - interpolationSettings.SlowDownSpeed;

            return 1;
        }

        // Этим методом мы ограничиваем, насколько локальное время может опережать или отставать от TargetTime
        // TargetTime отстаёт от RemoteTime на размер буфера
        // Если вдруг снапшоты перестали приходить, то локальное время не убежит вперёд
        // Если после паузы пришли новые снапшоты, у которых RemoteTime убежало вперёд, будет скачок в локальном времени, чтобы быстро их догнать
        private static float TimelineClamp(
            float localTimeline, // Локальное время
            float bufferTime, // Искусственное отставание буфера для интерполяции
            float latestRemoteTime) // Последнее RemoteTime время, которое пришло в снапшоте
        {
            float targetTime = latestRemoteTime - bufferTime;
            float lowerBound = targetTime - bufferTime; // how far behind we can get
            float upperBound = targetTime + bufferTime; // how far ahead we can get

            return Math.Clamp(localTimeline, lowerBound, upperBound);
        }

        private static void RemoveRange<T, U>(this SortedList<T, U> list, int amount)
        {
            for (int i = 0; i < amount && i < list.Count; ++i)
                list.RemoveAt(0);
        }
    }
}
