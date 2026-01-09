using FishNet.Object;
using SkillcadeSDK;
using SkillcadeSDK.Replays;
using SkillcadeSDK.Replays.Components;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Replays
{
    public class ReplayColorSetter : MonoBehaviour
    {
        [SerializeField] private ReplayObjectHandler _replayObjectHandler;
        [SerializeField] private NetworkObject _networkObject;
        [SerializeField] private SpriteRenderer[] _targerRenderers;
        [SerializeField] private Graphic[] _targetGraphics;
        [SerializeField] private GameObject[] _replayObjects;
        
        [Inject] private readonly IObjectResolver _objectResolver;

        private bool _subscribed;
        private ReplayClientWorld _clientWorld;

        private void Start()
        {
            this.InjectToMe();
        }

        private void Update()
        {
            if (_subscribed)
                return;
            
            if (_networkObject.IsSpawned)
            {
                _subscribed = true;
                _replayObjects.SetActive(false);
                return;
            }
            
            if (!_replayObjectHandler.IsReplaying)
                return;
            
            if (!_objectResolver.TryResolve(out ReplayReadService readService))
                return;
            
            if (!readService.ClientWorlds.TryGetValue(_replayObjectHandler.WorldId, out _clientWorld))
                return;

            _subscribed = true;
            _replayObjects.SetActive(true);
            _clientWorld.OnColorChanged += UpdateColor;
        }

        private void OnDisable()
        {
            if (!_subscribed || _clientWorld == null)
                return;
            
            _clientWorld.OnColorChanged -= UpdateColor;
        }

        private void OnDestroy()
        {
            if (!_subscribed || _clientWorld == null)
                return;
            
            _clientWorld.OnColorChanged -= UpdateColor;
        }

        private void UpdateColor()
        {
            SetColor(_clientWorld.Color);
        }

        private void SetColor(Color color)
        {
            foreach (var targerRenderer in _targerRenderers)
            {
                targerRenderer.color = SetColorValuesWithoutAlpha(targerRenderer.color, color);
            }

            foreach (var graphic in _targetGraphics)
            {
                graphic.color = SetColorValuesWithoutAlpha(graphic.color, color);
            }
        }

        private static Color SetColorValuesWithoutAlpha(Color target, Color source)
        {
            target.r = source.r;
            target.g = source.g;
            target.b = source.b;
            return target;
        }
    }
}