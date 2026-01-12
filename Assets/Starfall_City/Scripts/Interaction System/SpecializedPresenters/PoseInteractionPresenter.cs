using UnityEngine;
using core;

namespace Interaction
{
    public class PoseInteractionPresenter : InteractionPresenter
    {
        [SerializeField] private PoseConfig _poseConfig;
        [SerializeField] private Transform _targetPosition;
        [SerializeField] private Vector3 _rotationOffset = Vector3.zero;

        protected override void PerformInteraction()
        {
            if (_poseConfig == null) return;

            Vector3 targetPos = _targetPosition != null ? _targetPosition.position : transform.position;
            float targetYRot = _targetPosition != null
                ? _targetPosition.rotation.eulerAngles.y + _rotationOffset.y
                : transform.rotation.eulerAngles.y;

            PosePresenter.Instance.EnterPose(targetPos, targetYRot, _poseConfig);
        }

        public override string GetIdentifier() => _poseConfig != null ? _poseConfig.poseID : null ?? base.GetIdentifier();
    }
}
