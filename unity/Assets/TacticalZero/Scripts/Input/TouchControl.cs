using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TacticalZero
{
    public sealed class TouchControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private static readonly Dictionary<int, TouchControl> owners = new Dictionary<int, TouchControl>();
        [SerializeField]
        private MobileAction action;
        private int pointer = int.MinValue;
        private Vector2 origin;
        private MobileInputBridge bridge;
        private RectTransform knob;
        public void Configure(MobileAction type, RectTransform handle = null)
        {
            action = type;
            knob = handle;
            if(type!=MobileAction.Look&&transform is RectTransform&&transform.parent is RectTransform)gameObject.AddComponent<TouchLayout>().Configure(type);
        }

        public void OnPointerDown(PointerEventData data)
        {
            var game = GameManager.Instance;
            if (!game || !game.IsPlaying || TouchLayout.Editing || pointer != int.MinValue || owners.ContainsKey(data.pointerId))
                return;
            pointer = data.pointerId;
            owners[pointer] = this;
            origin = data.position;
            bridge = game.Input.Mobile;
            switch (action)
            {
                case MobileAction.Fire:
                    bridge.Fire = true;
                    bridge.Press(action);
                    break;
                case MobileAction.Aim:
                    bridge.Aim = !bridge.Aim;
                    break;
                case MobileAction.Sprint:
                    bridge.Sprint = true;
                    break;
                case MobileAction.Crouch:
                    bridge.Crouch = !bridge.Crouch;
                    break;
                case MobileAction.Move:
                    bridge.Move = Vector2.zero;
                    break;
                case MobileAction.Look:
                    break;
                default:
                    bridge.Press(action);
                    break;
            }
        }

        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != pointer || bridge == null)
                return;
            float scale = GetComponentInParent<Canvas>().scaleFactor;
            if (action == MobileAction.Move)
            {
                var value = Vector2.ClampMagnitude((data.position - origin) / (70 * scale), 1);
                bridge.Move = value;
                if (knob)
                    knob.anchoredPosition = value * 48;
            }

            if (action == MobileAction.Look)
                bridge.LookDelta += data.delta / scale;
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.pointerId == pointer)
                Release();
        }

        private void Release()
        {
            if (pointer == int.MinValue)
                return;
            owners.Remove(pointer);
            pointer = int.MinValue;
            if (bridge != null)
            {
                if (action == MobileAction.Move)
                    bridge.Move = Vector2.zero;
                if (action == MobileAction.Fire)
                    bridge.Fire = false;
                if (action == MobileAction.Sprint)
                    bridge.Sprint = false;
                if (action == MobileAction.Look)
                    bridge.LookDelta = Vector2.zero;
            }

            if (knob)
                knob.anchoredPosition = Vector2.zero;
        }

        private void OnDisable()
        {
            Release();
        }

        public static void ReleaseAll()
        {
            // Release individual ownership as well as values in MobileInputBridge.
            while(owners.Count>0)
            {
                TouchControl owner=null;int id=0;
                foreach(var pair in owners){owner=pair.Value;id=pair.Key;break;}
                if(owner)owner.Release();else owners.Remove(id);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRuntimeState()
        {
            owners.Clear();
        }
    }
}
