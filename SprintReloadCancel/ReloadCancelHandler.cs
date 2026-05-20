using Player;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SprintReloadCancel
{
    public sealed class ReloadCancelHandler : MonoBehaviour
    {
        public static ReloadCancelHandler Instance { get; private set; } = null!;
        public ReloadCancelHandler(IntPtr ptr) : base(ptr) { }

        enum CancelState
        {
            Reload,
            SwapBack,
            SwapBuffer
        }

        private PlayerAgent? _owner;
        private ItemEquippable? _item;
        private CancelState _currentState = CancelState.Reload;
        private InventorySlot _swapSlot = InventorySlot.None;
        private float _reloadEndTime = 0f;
        private bool _bufferPush = false;
        private float _bufferEndTime = 0f;

        private const float SwapTime = 0.2f;
        private static readonly Dictionary<InputAction, InventorySlot> SwapActions = new() {
            { InputAction.SelectStandard, InventorySlot.GearStandard },
            { InputAction.SelectSpecial, InventorySlot.GearSpecial },
            { InputAction.SelectTool, InventorySlot.GearClass },
            { InputAction.SelectMelee, InventorySlot.GearMelee },
            { InputAction.SelectConsumable, InventorySlot.Consumable },
            { InputAction.SelectHackingTool, InventorySlot.HackingTool },
            { InputAction.SelectResourcePack, InventorySlot.ResourcePack },
        };

        private void Awake()
        {
            Instance = this;
            enabled = false;
        }

        private void ClearState()
        {
            _item = null;
            _owner = null;
            _currentState = CancelState.Reload;
            _swapSlot = InventorySlot.None;
            _bufferPush = false;
            enabled = false;
        }

        public void OnReloadStart(ItemEquippable item, float reloadEndTime)
        {
            var owner = item.Owner;
            if (owner == null || !owner.IsLocallyOwned) return;

            _item = item;
            _owner = owner;
            _reloadEndTime = reloadEndTime;
            _currentState = CancelState.Reload;
            enabled = true;
        }

        private void Update()
        {
            if (_owner == null || _item == null)
            {
                ClearState();
                return;
            }

            switch (_currentState)
            {
                case CancelState.Reload:
                    if (!_item.IsReloading)
                    {
                        ClearState();
                        return;
                    }

                    if (!ShouldCancel()) return;

                    _swapSlot = _owner!.FPItemHolder.m_inventoryLocal.WieldedSlot;
                    _owner.FPItemHolder.MeleeAttackShortcut();
                    _currentState = CancelState.SwapBack;
                    break;
                case CancelState.SwapBack:
                    _owner.Sync.WantsToWieldSlot(_swapSlot);

                    if (Configuration.SwapBuffer)
                    {
                        _currentState = CancelState.SwapBuffer;
                        _bufferEndTime = Clock.Time + SwapTime;
                    }
                    else
                        ClearState();
                    break;
                case CancelState.SwapBuffer:
                    CheckSwapBuffer();
                    break;
            }
        }

        private bool ShouldCancel()
        {
            var filter = _owner!.InputFilter;
            return (Configuration.SprintCancelEnabled && InputMapper.GetButtonDown.Invoke(InputAction.Run, filter) && _owner.Locomotion.InputIsForwardEnoughForRun())
                || (_reloadEndTime - SwapTime > Clock.Time && ( // Avoid canceling reloads that are almost done (e.g. spamming shoot as it finishes)
                      (Configuration.AimCancelEnabled && InputMapper.GetButtonDown.Invoke(InputAction.Aim, filter))
                   || (Configuration.ShootCancelEnabled && InputMapper.GetButtonDown.Invoke(InputAction.Fire, filter))
                   ));
        }

        private void CheckSwapBuffer()
        {
            var filter = _owner!.InputFilter;
            // Check for swap attempts until we can swap again
            if (Clock.Time < _bufferEndTime)
            {
                foreach (var pair in SwapActions)
                {
                    if (InputMapper.GetButtonDown.Invoke(pair.Key, filter))
                    {
                        _swapSlot = pair.Value;
                        _bufferPush = false;
                    }
                }

                if (InputMapper.GetButtonDown.Invoke(InputAction.Melee, filter))
                    _bufferPush = true;

                return;
            }

            if (_owner.FPItemHolder.m_inventoryLocal.WieldedSlot != InventorySlot.InLevelCarry)
            {
                if (_bufferPush)
                    _owner.FPItemHolder.MeleeAttackShortcut();
                else if (_swapSlot != InventorySlot.None)
                    _owner.Sync.WantsToWieldSlot(_swapSlot);
            }

            ClearState();
        }
    }
}
