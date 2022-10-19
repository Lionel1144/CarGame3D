using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drive : MonoBehaviour
{
    public WheelCollider[] WCs;
    public GameObject[] Wheels;
    public float torque = 200;
    public float maxStreerAngle = 30;
    public float maxBrakeTorque = 500;
    public GameObject Wheel;

    public AudioSource skidSound;
    public AudioSource highAccel;

    public Transform SkidTrailPrefab;
    Transform[] skidTrails = new Transform[4];

    public ParticleSystem smokePrefab;
    ParticleSystem[] skidSmoke = new ParticleSystem[4];

    public GameObject brakeLight;

    public Rigidbody rb;
    public float gearLengh = 3;
    public float currentSpeed
    {
        get
        {
            return rb.velocity.magnitude * gearLengh;
        }
    }
    public float lowPitch = 1f;
    public float highPitch = 6f;
    public int numGears = 5;
    float rpm;
    int CurrentGear = 1;
    float currentGearPerc;
    public float maxSpeed = 200;

    public void StartSkidTrail(int i)
    {
        if (skidTrails[i] == null)
        {
            skidTrails[i] = Instantiate(SkidTrailPrefab);
        }

        skidTrails[i].parent = WCs[i].transform;
        skidTrails[i].localRotation = Quaternion.Euler(90, 0, 0);
        skidTrails[i].localPosition = -Vector3.up * WCs[i].radius;
    }

    public void EndskidTrail(int i)
    {
        if (skidTrails[i] == null) 
        {
            return;
        }
        Transform holder = skidTrails[i];
        skidTrails[i] = null;
        holder.parent = null;
        holder.rotation = Quaternion.Euler(90, 0, 0);
        Destroy(holder.gameObject, 30);
    }

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            skidSmoke[i] = Instantiate(smokePrefab);
            skidSmoke[i].Stop();
        }
        brakeLight.SetActive(false);
    }

    public void CalEngineSound()
    {
        float gearPercentage = (1 / (float)numGears);
        float targetGearFactor = Mathf.InverseLerp(gearPercentage * CurrentGear, gearPercentage * (CurrentGear + 1), Mathf.Abs(currentSpeed / maxSpeed));
        currentGearPerc = Mathf.Lerp(currentGearPerc, targetGearFactor, Time.deltaTime * 5f);

        var gearNumFactor = CurrentGear / (float)numGears;
        rpm = Mathf.Lerp(gearNumFactor, 1, currentGearPerc);

        float speedPerc = Mathf.Abs(currentSpeed / maxSpeed);
        float upperGearMax = (1 / (float)numGears) * (CurrentGear + 1);
        float downGearMax = (1 / (float)numGears) * CurrentGear;

        if (CurrentGear > 0 && speedPerc < downGearMax)
        {
            CurrentGear--;
        }

        if (speedPerc > upperGearMax && (CurrentGear < (numGears - 1)))
        {
            CurrentGear++;
        }

        float pitch = Mathf.Lerp(lowPitch, highPitch, rpm);
        highAccel.pitch = Mathf.Min(highPitch, pitch) * 0.25f;
    }

    public void Go(float accel, float steer, float brake)
    {
        accel = Mathf.Clamp(accel, -1, 1);
        steer = Mathf.Clamp(steer, -1, 1) * maxStreerAngle;
        brake = Mathf.Clamp(brake, 0, 1) * maxBrakeTorque;

        if (brake != 0 )
        {
            brakeLight.SetActive(true);
        }
        else
        {
            brakeLight.SetActive(false);
        }

        float thrustTorque = 0;  
        if(currentSpeed < maxSpeed)
        {
            thrustTorque = accel * torque;
        }

        for (int i = 0; i < 4; i++)
        {
            WCs[i].motorTorque = thrustTorque;

            if (i < 2)
            {
                WCs[i].steerAngle = steer;
            }
            else
            {
                WCs[i].brakeTorque = brake;
            }

            Quaternion quat;
            Vector3 position;
            WCs[i].GetWorldPose(out position, out quat);
            Wheels[i].transform.position = position;
            Wheels[i].transform.rotation = quat;
        }
    }

    public void CheckForSkid()
    {
        int numSkidding = 0;
        for (int i = 0; i < 4; i++)
        {
            WheelHit wheelHit;
            WCs[i].GetGroundHit(out wheelHit);

            if (Mathf.Abs(wheelHit.forwardSlip) >= 0.4f || Mathf.Abs(wheelHit.sidewaysSlip) >= 0.4f)
            {
                numSkidding++;
                if (!skidSound.isPlaying)
                {
                    skidSound.Play();
                }
                StartSkidTrail(i);
                skidSmoke[i].transform.position = WCs[i].transform.position - WCs[i].transform.up * WCs[i].radius;
                skidSmoke[i].Emit(1);
            }
            else
            {
                EndskidTrail(i);
            }
        }
        if(numSkidding == 0 && skidSound.isPlaying)
        {
            skidSound.Stop();
        }
    }

}
