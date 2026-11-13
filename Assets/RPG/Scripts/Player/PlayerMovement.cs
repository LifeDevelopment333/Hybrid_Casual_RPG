using Rpg.Player;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("옵션")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float acceleration = 20f;

    [Header("참조")]
    [SerializeField] private Transform mainCam;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private Animator animator;
    private PlayerAuto playerAuto;

    private Vector3 moveDirection;
    private Vector3 horizontalVelocity;
    private float verticalSpeed = -2f;      // 낙하 속도

    private static readonly int Speed = Animator.StringToHash("Speed");

    private void Awake()
    {
        if (mainCam == null)
            mainCam = Camera.main.transform;

        if(characterController == null)
            characterController = transform.GetComponent<CharacterController>();

        if(animator == null)
            animator = GetComponentInChildren<Animator>();

        playerAuto = transform.GetComponent<PlayerAuto>();
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        Vector2 input = joystick ? Vector2.ClampMagnitude(joystick.Value, 1) : Vector2.zero;

        // 카메라 방향 기준 입력 방향 보정
        Vector3 forward = mainCam ? Vector3.ProjectOnPlane(mainCam.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 right = mainCam ? Vector3.ProjectOnPlane(mainCam.right, Vector3.up).normalized : Vector3.right;

        moveDirection = (forward * input.y) + (right * input.x);
        if(playerAuto != null)
        {
            bool hasInput = input.sqrMagnitude > 0.001f;
            Vector3 autoDirection = playerAuto.AutoMovement(hasInput);

            if (hasInput == false)
                moveDirection = autoDirection;
        }

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, moveDirection * moveSpeed, acceleration * Time.deltaTime);

        // 캐릭터 회전
        if (moveDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moveDirection), turnSpeed * Time.deltaTime);

        // 캐릭터 움직임
        characterController.Move((horizontalVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);

        float actualSpeed = Vector3.ProjectOnPlane(characterController.velocity, Vector3.up).magnitude;
        if(animator != null) animator.SetFloat(Speed, Mathf.Clamp01(actualSpeed / moveSpeed), 0.1f, Time.deltaTime);
    }
}
