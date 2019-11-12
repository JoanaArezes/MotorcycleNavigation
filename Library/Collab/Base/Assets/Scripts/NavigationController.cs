using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;
using System;

public class NavigationController : MonoBehaviour { 

    private string[,] routeInfo = new string[9,4];
    private int curRoute = 0;
    private string nextIntersection;
    private int step = 0;
    private int format;
    private Vector3 originalPos;
    private bool navigating;
    private float startTime;

    // line rendering variables
    private LineRenderer lineRenderer;
    private float counter;
    private Vector3 origin;
    private Vector3 destination;
    private Vector3 p0;
    private Vector3 p1;
    private Vector3 p2;
    private bool drawing = false;
    private int pos = 1;

    public string formatOrder;
    public int distance;
    public Image HUD;
    public GameObject line;

    void Start()
    {
        originalPos = transform.position;
        startTime = Time.realtimeSinceStartup;

        // choose route according to format
        curRoute = formatOrder.Equals("up") ? 0 : 7;
        UpdateFormatRoute();

        // linerendering setup
        lineRenderer = line.GetComponent<LineRenderer>();
        //lineRenderer.widthMultiplier = 0.6f;
        resetDirectionLine();

        // start first route
        navigating = true;
    }

    void Update()
    {
        if (!navigating)
            if (Input.GetKeyDown(KeyCode.Space) || Input.anyKey)
            {
                navigating = true;
                transform.position = originalPos;
                transform.rotation = Quaternion.identity;
                this.GetComponent<DriverController>().NullSpeed();

                //UpdateHUDSprite("formats/empty");
                startTime = Time.realtimeSinceStartup;
            }

        if (navigating)
            if (Vector3.Distance(transform.position, GameObject.Find(nextIntersection).transform.position) < distance)
            {
                //show new direction
                if (!routeInfo[step, 2].Equals("none")) {
                    /*
                    if (format == 3) // this format has specific intersection info
                        UpdateHUDSprite("formats/" + format.ToString() + "/" + routeInfo[step, 3] + "-" + routeInfo[step, 2]);
                    else
                        UpdateHUDSprite("formats/" + format.ToString() + "/" + routeInfo[step, 2]);
                    */
                    drawDirectionLine();
                }
            }
    }

    private void OnCollisionExit(Collision collision)
    {   
        if (navigating)
            if (collision.gameObject.name == nextIntersection)
            {
                // take down direction
                //UpdateHUDSprite("formats/empty");
                resetDirectionLine();

                // next intersection
                step++;
                if (step < 8)
                   nextIntersection = routeInfo[step, 0] + "+" + routeInfo[step, 1];
                else
                   UpdateFormatRoute();

                Debug.Log("next intersection: " + nextIntersection + "\t direction: " + routeInfo[step, 2]);
            }
    }
   
    private void UpdateFormatRoute()
    {
        WriteInfo(Time.realtimeSinceStartup - startTime);
        curRoute = formatOrder.Equals("up") ? curRoute+1 : curRoute-1; 

        if (curRoute > 6 || curRoute < 1)
        {
            navigating = false;
            return;
        }  

        format = (int)Mathf.RoundToInt((curRoute+0.5f)/2);

        // reset for new route
        ParseFile(curRoute);
        step = 0;
        nextIntersection = routeInfo[step, 0] + "+" + routeInfo[step, 1];

        // pause navigation system
        navigating = false;
        //UpdateHUDSprite("formats/routeend");

        Debug.Log("format: " + format.ToString() + "\nroute: " + curRoute.ToString());
    }

    private void WriteInfo(float time)
    {
        string path = "Assets/Resources/routeinfo.txt";
        StreamWriter writer = new StreamWriter(path, true);
        string info = curRoute.ToString() + "\t" + time.ToString();

        writer.WriteLine(info);
        writer.Close();
    }

    private void ParseFile(int route)
    {
        string path = "Assets/Resources/routes/" + route.ToString() + ".txt";
        StreamReader reader = new StreamReader(path);
        char[] delimiterChars = { ' ', '\t'};

        string line = reader.ReadLine();
        int step = 0;
        while (!string.IsNullOrEmpty(line))
        {
            string[] info = line.Split(delimiterChars);
            for (int i = 0; i < 4; i++)
            {
                routeInfo[step, i] = info[i];
            }
            line = reader.ReadLine();
            step++;
        }
        reader.Close();
    }

    private void drawDirectionLine()
    {
        if (!drawing) {
            float y = 0.2f;
            float yInterval = y + 0.4f;
            float delta = 1.2f;
            float startPoint = 25f;
            float endPoint = 6f;

            int i = (step == 0 ? step : step - 1);

            Vector3 prev_intersection = GameObject.Find(routeInfo[i, 0] + "+" + routeInfo[i, 1]).transform.GetComponent<Renderer>().bounds.center;
            Vector3 next_intersection = GameObject.Find(nextIntersection).transform.GetComponent<Renderer>().bounds.center;
            Vector3 direction = next_intersection - prev_intersection;

            if (Math.Abs(direction.x) > Math.Abs(direction.z)) {
                // direction is x
                int way = (direction.x > 0 ? 1 : -1);

                p0 = new Vector3(next_intersection.x - startPoint * way, y, next_intersection.z - delta * way);

                if (routeInfo[step, 2].Equals("forward")) {
                    p2 = p1 = new Vector3(next_intersection.x, yInterval, next_intersection.z - delta * way);
                    p2.x += endPoint * way;
                }
                else if (routeInfo[step, 2].Equals("right")) {
                    p2 = p1 = new Vector3(next_intersection.x - delta * way, yInterval, next_intersection.z - delta * way);
                    p2.z -= endPoint * way;
                }
                else if (routeInfo[step, 2].Equals("left")) {
                    p2 = p1 = new Vector3(next_intersection.x + delta * way, yInterval, next_intersection.z - delta * way);
                    p2.z += endPoint * way;
                }
            }
            else {
                // direction is z
                int way = (direction.z >= 0 ? 1 : -1);

                p0 = new Vector3(next_intersection.x + delta * way, y, next_intersection.z - startPoint * way);

                if (routeInfo[step, 2].Equals("forward")) {
                    p2 = p1 = new Vector3(next_intersection.x + delta * way, yInterval, next_intersection.z);
                    p2.z += endPoint * way;
                }
                else if (routeInfo[step, 2].Equals("right")) {
                    p2 = p1 = new Vector3(next_intersection.x + delta * way, yInterval, next_intersection.z - delta * way);
                    p2.x += endPoint * way;
                }
                else if (routeInfo[step, 2].Equals("left")) {
                    p2 = p1 = new Vector3(next_intersection.x + delta * way, yInterval, next_intersection.z + delta * way);
                    p2.x -= endPoint * way;
                }
            }
            Debug.Log("p0: " + p0.ToString() + " p1: " + p1.ToString() + " p2: " + p2.ToString());

            origin = p0;
            destination = p1;

            lineRenderer.SetPosition(0, origin);
            drawing = true;
            counter = 0;
            pos = 1;
        }

        if (counter >= 1 && lineRenderer.positionCount == 2) {
            origin = p1;
            destination = p2;
            counter = 0;
            pos = 2;
            lineRenderer.positionCount = 3;
        }

        counter += 0.1f / 2f;
        lineRenderer.SetPosition(pos, Vector3.Lerp(origin, destination, counter));
    }

    private void resetDirectionLine()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, Vector3.zero);
        drawing = false;
    }

    private void UpdateHUDSprite(string sprite)
    {
        HUD.sprite = Resources.Load<Sprite>(sprite);
    }
}

/*     // PRINTING ARRAY
       int rowLength = routeInfo.GetLength(0);
       int colLength = routeInfo.GetLength(1);
       string print = "";
       for (int i = 0; i < rowLength; i++)
       {
           for (int j = 0; j < colLength; j++)
           {
               print += routeInfo[i, j] + "\t";
           }
           print += "\n";
       }
       Debug.Log(print);*/
