using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class CarController : MonoBehaviour
{
    #region Variables

    [Header("Movement")] public List<AxleInfo> axleInfos;
    [SerializeField] private float forwardMotorTorque = 100000;
    [SerializeField] private float backwardMotorTorque = 50000;
    [SerializeField] private float maxSteeringAngle = 15;
    [SerializeField] private float engineBrake = 1e+12f;
    [SerializeField] private float footBrake = 1e+24f;
    [SerializeField] private float topSpeed = 200f;
    [SerializeField] private float downForce = 100f;
    [SerializeField] private float slipLimit = 0.2f;

    [SerializeField] private bool isDumped = false;

    private bool waiting = false;

    public int ID;

    private float CurrentRotation { get; set; }
    public float InputAcceleration { get; set; }
    public float InputSteering { get; set; }
    public float InputBrake { get; set; }

    // Se hace accesible su rigidbody para poder resetear las físicas del coche mientras se reposiciona
    public Rigidbody _rigidbody;
    private float _steerHelper = 0.8f;


    public float _currentSpeed = 0;

    // Estos parámetros se utilizan para controlar que el coche no se quede atascado o volcado
    private const float VELOCIDAD_MINIMA = 0.1f;
    private const float MAX_TIEMPO_VOLCADO = 2f;
    private const float MAX_TIEMPO_ATASCADO = 5f;
    public float tiempoParado = 0f;
    // Variable para indicar que se está reposicionando al coche
    public bool volviendoCheckpoint = false;

    public bool IsOwner = false; // Esta variable se encarga de indicar si el coche es del cliente que está jugando
    // Variables que controlan la clasificación y el flujo de ejecución en función de ello
    public bool esperandoClasificacion = false;
    public bool clasificacionIniciada = false;
    private float Speed
    {
        get => _currentSpeed;
        set
        {
            if (Math.Abs(_currentSpeed - value) < float.Epsilon) return;
            _currentSpeed = value;
            if (OnSpeedChangeEvent != null)
                OnSpeedChangeEvent(_currentSpeed);
        }
    }

    public delegate void OnSpeedChangeDelegate(float newVal);

    public event OnSpeedChangeDelegate OnSpeedChangeEvent;

    // Lista de colliders de las ruedas, para detectar si se sale fuera del circuito
    public List<WheelCollider> wheelColliders;

    // Gestión del rubber band
    public float factorAceleracion;

    #endregion Variables

    #region Unity Callbacks

    public void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Update()
    {
        // Si ya no se está esperando a la clasificación, se marca como falso
        if(!RaceController.instance.clasificacion)
        {
            esperandoClasificacion = false;
        }
        Speed = _rigidbody.velocity.magnitude;
        // Se mantiene al coche quieto si se está esperando a que los demás terminen la clasificación
        if(esperandoClasificacion)
        {
            _rigidbody.velocity = Vector3.zero;
        }
        // COMPROBACIÓN DE SI EL COCHE SE HA QUEDADO INMOVILIZADO POR UN CHOQUE O POR HABER VOLCADO
        if(Speed <= VELOCIDAD_MINIMA && !this.isDumped && !esperandoClasificacion)
        {
            tiempoParado+= Time.deltaTime;
            // Si está más tiempo parado del que se permite, se comprueba primero si ha volcado
            if(tiempoParado >= MAX_TIEMPO_VOLCADO)
            {
                CheckDump();
            }
            // Si no está volcado, pero está inmovilizado en alguna parte, se resetea su estado y se envía al último checkpoint
            if(tiempoParado >= MAX_TIEMPO_ATASCADO)
            {
                // Se reinicia el tiempo parado
                tiempoParado = 0f;
                CheckPointPlayer checkPoint = GetComponent<CheckPointPlayer>();
                if (checkPoint != null)
                {
                    // Se ejecuta la función de respawn con -1, para hacer que se vuelva al 100% al checkpoint anterior
                    checkPoint.OnCheckPointPassedServerRpc(-1, ID);
                }
            }
        }
        else
        {
            tiempoParado = 0f;
        }

        // También se comprueba si el coche está fuera del circuito
        OutOfCircuit();
        // Se aplica la técnica rubber band, que modifica la velocidad del coche en función de la posición, utilizando un factor en su aceleración
        RubberBand();
    }

    public void FixedUpdate() 
    {
        InputSteering = Mathf.Clamp(InputSteering, -1, 1);
        InputAcceleration = Mathf.Clamp(InputAcceleration, -1, 1);
        // Se aplica el rubber band
        InputAcceleration *= factorAceleracion;
        InputBrake = Mathf.Clamp(InputBrake, 0, 1);

        float steering = maxSteeringAngle * InputSteering;

        foreach (AxleInfo axleInfo in axleInfos)
        {
            if (axleInfo.steering)
            {
                axleInfo.leftWheel.steerAngle = steering;
                axleInfo.rightWheel.steerAngle = steering;
            }

            if (axleInfo.motor)
            {
                if (InputAcceleration > float.Epsilon)
                {
                    axleInfo.leftWheel.motorTorque = forwardMotorTorque;
                    axleInfo.leftWheel.brakeTorque = 0;
                    axleInfo.rightWheel.motorTorque = forwardMotorTorque;
                    axleInfo.rightWheel.brakeTorque = 0;
                }

                if (InputAcceleration < -float.Epsilon)
                {
                    axleInfo.leftWheel.motorTorque = -backwardMotorTorque;
                    axleInfo.leftWheel.brakeTorque = 0;
                    axleInfo.rightWheel.motorTorque = -backwardMotorTorque;
                    axleInfo.rightWheel.brakeTorque = 0;
                }

                if (Math.Abs(InputAcceleration) < float.Epsilon)
                {
                    axleInfo.leftWheel.motorTorque = 0;
                    axleInfo.leftWheel.brakeTorque = engineBrake;
                    axleInfo.rightWheel.motorTorque = 0;
                    axleInfo.rightWheel.brakeTorque = engineBrake;
                }

                if (InputBrake > 0)
                {
                    axleInfo.leftWheel.brakeTorque = footBrake;
                    axleInfo.rightWheel.brakeTorque = footBrake;
                }
            }

            ApplyLocalPositionToVisuals(axleInfo.leftWheel);
            ApplyLocalPositionToVisuals(axleInfo.rightWheel);
        }

        //mejorar la conduccion
        SteerHelper();
        SpeedLimiter();
        AddDownForce();
        TractionControl();

        // Si ha volcado, inicia la co-rutina y vuelve a poner el booleano de volcado a false
        if (isDumped)
        {
            StartCoroutine(WaitForReset());
            isDumped = false;
        }

    }

    #endregion

    #region Methods

    // crude traction control that reduces the power to wheel if the car is wheel spinning too much
    private void TractionControl()
    {
        foreach (var axleInfo in axleInfos)
        {
            WheelHit wheelHitLeft;
            WheelHit wheelHitRight;
            axleInfo.leftWheel.GetGroundHit(out wheelHitLeft);
            axleInfo.rightWheel.GetGroundHit(out wheelHitRight);

            if (wheelHitLeft.forwardSlip >= slipLimit)
            {
                var howMuchSlip = (wheelHitLeft.forwardSlip - slipLimit) / (1 - slipLimit);
                axleInfo.leftWheel.motorTorque -= axleInfo.leftWheel.motorTorque * howMuchSlip * slipLimit;
            }

            if (wheelHitRight.forwardSlip >= slipLimit)
            {
                var howMuchSlip = (wheelHitRight.forwardSlip - slipLimit) / (1 - slipLimit);
                axleInfo.rightWheel.motorTorque -= axleInfo.rightWheel.motorTorque * howMuchSlip * slipLimit;
            }
        }
    }

// this is used to add more grip in relation to speed
    private void AddDownForce() //fuerza para que no vuelque mucho
    {
        foreach (var axleInfo in axleInfos)
        {
            axleInfo.leftWheel.attachedRigidbody.AddForce(
                -transform.up * (downForce * axleInfo.leftWheel.attachedRigidbody.velocity.magnitude));
        }
    }

    private void SpeedLimiter()
    {
        float speed = _rigidbody.velocity.magnitude;
        if (speed > topSpeed)
            _rigidbody.velocity = topSpeed * _rigidbody.velocity.normalized;
    }

// finds the corresponding visual wheel
// correctly applies the transform
    public void ApplyLocalPositionToVisuals(WheelCollider col)
    {
        if (col.transform.childCount == 0)
        {
            return;
        }

        Transform visualWheel = col.transform.GetChild(0);
        Vector3 position;
        Quaternion rotation;
        col.GetWorldPose(out position, out rotation);
        var myTransform = visualWheel.transform;
        myTransform.position = position;
        myTransform.rotation = rotation;
    }

    private void SteerHelper()
    {
        foreach (var axleInfo in axleInfos)
        {
            WheelHit[] wheelHit = new WheelHit[2];
            axleInfo.leftWheel.GetGroundHit(out wheelHit[0]);
            axleInfo.rightWheel.GetGroundHit(out wheelHit[1]);
            foreach (var wh in wheelHit)
            {
                if (wh.normal == Vector3.zero)
                    return; // wheels arent on the ground so dont realign the rigidbody velocity
            }
        }

// this if is needed to avoid gimbal lock problems that will make the car suddenly shift direction
        if (Mathf.Abs(CurrentRotation - transform.eulerAngles.y) < 10f)
        {
            var turnAdjust = (transform.eulerAngles.y - CurrentRotation) * _steerHelper;
            Quaternion velRotation = Quaternion.AngleAxis(turnAdjust, Vector3.up);
            _rigidbody.velocity = velRotation * _rigidbody.velocity;
        }

        CurrentRotation = transform.eulerAngles.y;
    }

    // Método que actualiza el coche si ha volcado o no utilizando su rotación
    private void CheckDump()
    {
        if (Math.Abs(transform.rotation.eulerAngles.z) > 50 && !waiting && !EndingController.Instance.carreraFinalizada)
        {
            this.isDumped = true;
            tiempoParado = 0f;
        } 
    }

    // Corutina para esperar 3 segundos antes de volver a la posición una vez volcado
    private IEnumerator WaitForReset()
    {
        waiting = true;
        yield return new WaitForSeconds(1f);
        // Se va a la posición del último checkpoint visitado
        CheckPointPlayer checkPoint = GetComponent<CheckPointPlayer>();
        if (checkPoint != null)
        {
            checkPoint.OnCheckPointPassedServerRpc(-1, ID);
            waiting = false;
        }
    }

    private void OutOfCircuit()
    {
        // Para cada una de las ruedas, se detecta si están en contacto con terreno que no pertenece a la carretera del circuito. Con que haya una mal, se reinicia la posición del coche
        foreach (WheelCollider wheelCollider in wheelColliders)
        {
            if(DetectCollision(wheelCollider)) return;
        }
    }

    private bool DetectCollision(WheelCollider wheelCollider)
    {
        RaycastHit hit;
        Vector3 wheelPosition;
        Quaternion wheelRotation;

        // Obtener la posición actual de la rueda
        wheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);

        // Realizar un Raycast desde la posición de la rueda hacia el suelo
        if (Physics.Raycast(wheelPosition, -wheelCollider.transform.up, out hit, 0.5f))
        {
            if(hit.collider.tag == "Arena" || hit.collider.tag == "Cesped")
            {
                Debug.Log("Fuera del circuito");
                // Se va a la posición del último checkpoint visitado, ya que, se está intentando saltar parte del circuito
                CheckPointPlayer checkPoint = GetComponent<CheckPointPlayer>();
                if (checkPoint != null)
                {
                    // Se hace que se vuelva al último punto de control
                    checkPoint.OnCheckPointPassedServerRpc(-1, ID);
                }
                return true;
            }
        }
        return false;
    }


    public void OnTriggerEnter(Collider other)
    {
        // Cada vez que un coche atraviesa la meta
        if(other.gameObject.tag == "Meta")
        {
            // Si ese coche es el propietatio
            if(IsOwner)
            {
                // La primera vez que se atraviesa la meta en la clasificación es para hacer que empiece el temporizador
                if(RaceController.instance.clasificacion)
                {
                    if(!clasificacionIniciada)
                    {
                        clasificacionIniciada = true;
                        UI_Clasificacion.instance.EmpezarTemporizador();
                    }
                    // La segunda vez es para finalizar la clasificación
                    else if(clasificacionIniciada && !esperandoClasificacion)
                    {
                        UI_Clasificacion.instance.AvanzarVuelta();
                        _rigidbody.velocity = Vector3.zero; // Se frena al coche
                        esperandoClasificacion = true;
                    }
                }
                // Si no se está en clasificación, se avanza una vuelta en la carrera
                else
                {
                    UI_HUD.Instance.AvanzarVuelta();
                }
            }
        } 
    }

    private void RubberBand()
    {
        // Solo se aplica esto en la carrera, no tiene sentido hacerlo durante la clasificación
        if (RaceController.instance.clasificacion) factorAceleracion = 1;
        int miPosicion = -1;
        // Primero se calcula la posición en la que estás
        for(int i=0; i<RaceController.instance.posiciones.Count; i++)
        {
            if (RaceController.instance.posiciones[i] == ID)
            {
                miPosicion = i;
            }
        }

        switch(TestLobby.Instance.NUM_PLAYERS_IN_LOBBY)
        {
            case 2:
                // Si hay dos jugadores, se aumenta ligeramente la velocidad del último y se disminuye ligeramente la velocidad del primero
                // Si voy primero:
                if(miPosicion == 1)
                {
                    factorAceleracion = 0.9f;
                }
                // Si voy segundo:
                else
                {
                    factorAceleracion = 1.1f;
                }
                break;
            case 3:
                // Si hay tres jugadores, se aumenta ligeramente la velocidad del último, el segundo permanece tal cual y se disminuye ligeramente la velocidad del primero 
                // Si voy primero:
                if (miPosicion == 1)
                {
                    factorAceleracion = 0.9f;
                }
                else if (miPosicion == 2)
                {
                    factorAceleracion = 1f;
                }
                // Si voy segundo no se modifica la velocidad
                else if (miPosicion == 3)
                {
                    factorAceleracion = 1.1f;
                }
                break;
            case 4:
                // Si hay cuatro jugadores, se aumenta ligeramente la velocidad del último, el tercero permanece tal cual, el segundo disminuye ligeramente y el primero un poco más
                if (miPosicion == 1)
                {
                    factorAceleracion = 0.7f;
                }
                else if (miPosicion == 2)
                {
                    factorAceleracion = 0.9f;
                }
                else if (miPosicion == 3)
                {
                    factorAceleracion = 1f;
                }
                else if (miPosicion == 4)
                {
                    factorAceleracion = 1.1f;
                }
                break;
        }
    }

    #endregion
}