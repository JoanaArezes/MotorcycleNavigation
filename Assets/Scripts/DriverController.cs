using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class DriverController : MonoBehaviour {

    public float maxSpeed; // in m/s
    public float turningSpeed;
    public float rearSpeed;

    private float speed = 0;
    private float prev_av = 0;
    private bool joystick;
    private Text speedometer;

    private int frameCounter = 0;
    private StreamWriter posTracker;

    // Use this for initialization
    void Start ()
    {
        // set file to track pos
        string path = "Assets/Resources/tracker.txt";
        posTracker = new StreamWriter(path, true);

        // set speedometer HMD
        speedometer = GameObject.Find("speedometer").GetComponent<Text>(); 
    }

    // Update is called once per frame
    void Update()
    {
        
        float way; // turning side
        float axis_value;

        if (Input.GetJoystickNames().Length > 0) joystick = true;
        else joystick = false;

        // acceleration        
        if (joystick)
        {
            axis_value = -Input.GetAxis("JSVertical");

            if (axis_value == 0)
            { // no input
                speed -= 0.04f;
            }
            else if (axis_value > 0)
            { // throttle on 
                float diff = (axis_value - prev_av) * 3 * maxSpeed;     // manage accelaration magnitude proportionally to the input 
                if (diff > 0)
                { // accelerating
                    speed += diff * Time.deltaTime;
                }
                else if (diff < 0)
                { //decelarating
                    speed += diff * 0.01f * Time.deltaTime;
                }
            }
            else if (axis_value < 0)
            { // right brake on 
                speed += axis_value * 0.20f * Time.deltaTime;
            }
            if (Input.GetKey("joystick button 4"))
            { // left brake on
                speed -= 0.4f * Time.deltaTime;
            }

            prev_av = axis_value;
        }
        else
        { //keyboard
            if (Input.GetKey(KeyCode.W) || Input.GetKey("joystick button 2"))
                //accelerate
                speed += 0.5f;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey("joystick button 3"))
                // brake
                speed -= 0.5f;
            else
                // friction and engine braking effect
                speed -= 0.1f;
        }

        // speed control
        if (speed < 0) speed = 0.0f;
        else if (speed > maxSpeed) speed = maxSpeed;

        // rear
        if ((Input.GetKey(KeyCode.R) || Input.GetKey("joystick button 7")) && speed == 0.0f)
            speed = -rearSpeed;
        else if (Input.GetKeyUp(KeyCode.R) || Input.GetKeyUp("joystick button 7"))
            speed = 0;

        // rotation 
        way = Input.GetAxis("Horizontal");
        transform.Rotate(0, way * turningSpeed * Time.deltaTime, 0, Space.World);

        //position 
        Vector3 direction = transform.forward;
        direction.y = 0;
        transform.position += direction * speed * Time.deltaTime;

        // write speed on HDD in km/h
        speedometer.text = ((int)(speed * 3.6)).ToString();

        if (frameCounter > 10)
        {
            WriteTracker();
            frameCounter = -1;
        }
        frameCounter++;

    }

    private void WriteTracker()
    {
        string info = transform.position.x.ToString() + "\t" + transform.position.z.ToString() + "\t" + Time.realtimeSinceStartup.ToString() + "\t" + Input.GetAxis("Horizontal") + "\t" + Input.GetAxis("JSVertical") + "\t" + Input.GetKey("joystick button 4");
        posTracker.WriteLine(info);
    }

    void OnDestroy()
    {
        posTracker.Close();
    }


    public void NullSpeed()
    {
        speed = 0.0f;
    }

}
