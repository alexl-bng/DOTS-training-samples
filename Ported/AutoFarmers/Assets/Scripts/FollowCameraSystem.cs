using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class FollowCameraSystem : SystemBase
{
	private Transform _cameraTransform;
	private Vector3 _cameraBasePosition;
	private EntityQuery _farmerQuery;
	private Entity _followingEntity = Entity.Null;
	private Random _random;
	private float _zoom;

	private const float _kLerpRate = 0.3f;
	private const float _kFollowDistance = 20;
	private const float _kZoomRate = 3;

	protected override void OnCreate()
	{
		_farmerQuery = EntityManager.CreateEntityQuery(typeof(Farmer));

		_cameraTransform = GameObject.FindObjectOfType<Camera>().transform;
		_cameraBasePosition = _cameraTransform.position;

		_random = new Random(1234);
	}

    protected override void OnUpdate()
    {
		if (Input.GetKeyDown(KeyCode.Space))
		{
			if (_followingEntity == Entity.Null)
			{
				NativeArray<Entity> farmers = _farmerQuery.ToEntityArray(Allocator.TempJob);
				int selectedIndex = _random.NextInt(0, farmers.Length);
				_followingEntity = farmers[selectedIndex];

				farmers.Dispose();
			}
			else
			{
				_followingEntity = Entity.Null;
			}
		}

		_zoom -= Input.mouseScrollDelta.y * _kZoomRate;

        if (_followingEntity == Entity.Null)
        {
			Vector3 targetPos = _cameraBasePosition - _cameraTransform.forward * _zoom;
			_cameraTransform.position = Vector3.Lerp(_cameraTransform.position, targetPos, _kLerpRate);
		}
        else
        {
			Translation farmerTranslation = GetComponent<Translation>(_followingEntity);
			Vector3 farmerPos = (Vector3)farmerTranslation.Value;
			farmerPos.y = 0;
			Vector3 targetPos = farmerPos - (_cameraTransform.forward * (_kFollowDistance + _zoom));
	        _cameraTransform.position = Vector3.Lerp(_cameraTransform.position, targetPos, _kLerpRate);
		}
    }
}
