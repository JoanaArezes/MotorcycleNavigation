using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;
using System;

public class NavigationController : MonoBehaviour { 

    // general navigation variables
    private string[,] routeInfo = new string[9,4];
    private List<int> routes;
    private int curRoute = 0;
    private string curStreet = "0th";
    private string nextIntersection;
    private int step = 0;
    private Vector3 originalPos;
    private bool navigating;
    private float startTime;
    private bool leftIntersection;

    // color controls
    private string curColor;

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
    private Vector2 originalAnchor;

    // public setup variables
    public string startWithColor; // green or white
    public string stabilization; // world or helmet
    public int noRoutes;
    public int distance;
    public Image HUD;

    public GameObject line;

    void Start()
    {
        originalPos = transform.position;
        startTime = Time.realtimeSinceStartup;

        // populate routes list with respective numbers
        routes = new List<int>();
        for (int i = 0; i < noRoutes; i++)
            routes.Add(i + 1);

        // linerendering setup
        lineRenderer = line.GetComponent<LineRenderer>();
        ResetDirectionLine();
        originalAnchor = HUD.rectTransform.anchoredPosition;

        //choose first route
        UpdateRoute();
        UpdateHUDSprite("formats/empty");

        // start system
        curColor = startWithColor;
        UpdateLineColor();

        navigating = true;
    }

    void Update()
    {
        if (!navigating)
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKey("joystick button 3") ) 
            {
                // reset driver
                transform.position = originalPos;
                transform.rotation = Quaternion.identity;
                this.GetComponent<DriverController>().NullSpeed();

                // reset system
                UpdateHUDSprite("formats/empty");
                startTime = Time.realtimeSinceStartup;
                navigating = true;
            }

        if (navigating)
            if (Vector3.Distance(transform.position, GameObject.Find(nextIntersection).transform.position) < distance)
            {
                leftIntersection = false;
                //show new direction
                if (!routeInfo[step, 2].Equals("none"))
                {
                    if (stabilization.Equals("world"))
                        DrawDirectionLine();
                    else if (stabilization.Equals("helmet"))
                        AnimateArrow("formats/2/" + curColor + "/" + routeInfo[step, 2]);
                }
            }
    }

    private void OnCollisionExit(Collision collision)
    {
        leftIntersection = true;
        if (navigating)
            if (collision.gameObject.name == nextIntersection)
            {
                // take down direction
                counter = 0;
                UpdateHUDSprite("formats/empty");
                ResetDirectionLine();

                // next intersection
                step++;
                if (step < 8)
                {
                    UpdateNextIntersection();
                }
                else
                { //route has ended
                    // pause navigation system
                    navigating = false;
                    UpdateRoute();
                }

                Debug.Log("Current Street: " + curStreet + "\nnext intersection: " + nextIntersection + "\t direction: " + routeInfo[step, 2]);
            }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (navigating && leftIntersection)
            if (collision.gameObject.transform.parent.name != curStreet)
                Debug.Log("Error: Driver made a wrong turn.");
    }

    private void UpdateRoute()
    {
        WriteInfo(Time.realtimeSinceStartup - startTime);
        
        if (noRoutes == 0)
        {   // turn off system
            navigating = false;
            UpdateHUDSprite("formats/testend");
            return;
        }
        else if (noRoutes == 3)
        {   // color update
            curColor = (curColor.Equals("white")) ? "green" : "white";
            UpdateLineColor();
        }

        // choose next random route
        int index = UnityEngine.Random.Range(0, noRoutes);
        curRoute = routes[index];

        routes.RemoveAt(index);
        noRoutes--;
            
        // reset for new route
        ParseFile(curRoute);
        step = 0;
        nextIntersection = routeInfo[step, 0] + "+" + routeInfo[step, 1];

        Debug.Log("route: " + curRoute.ToString());

        // warn user route is over, press to restart 
        UpdateHUDSprite("formats/routeend");
    }

    private void UpdateNextIntersection()
    {
        // update current street 
        if (routeInfo[step, 0].Equals(routeInfo[step - 1, 0]) || routeInfo[step, 0].Equals(routeInfo[step - 1, 1]))
            curStreet = routeInfo[step, 0];
        else
            curStreet = routeInfo[step, 1];

        nextIntersection = routeInfo[step, 0] + "+" + routeInfo[step, 1];
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

    private void AnimateArrow(string sprite)
    {
        counter += 0.1f;
        HUD.rectTransform.anchoredPosition = Vector2.Lerp(new Vector2(0, -250), originalAnchor, counter);

        UpdateHUDSprite(sprite);
    }

    private void UpdateHUDSprite(string sprite)
    {
        HUD.sprite = Resources.Load<Sprite>(sprite);
    }

    private void DrawDirectionLine()
    {
        Vector3 direction;
        Vector3 driver_coords = transform.position;

        if (!drawing) {
            float y = 0.2f;
            float yInterval = y + 0.4f;
            float delta = 1.2f;
            float endPoint = 6f;

            int i = (step == 0 ? step : step - 1);

            Vector3 prev_intersection = GameObject.Find(routeInfo[i, 0] + "+" + routeInfo[i, 1]).transform.GetComponent<Renderer>().bounds.center;
            Vector3 next_intersection = GameObject.Find(nextIntersection).transform.GetComponent<Renderer>().bounds.center;

            direction = next_intersection - prev_intersection;
            direction.Normalize();

            // calculate the 3 crucial points for the line: 
            //      p0 - starting point, p1 - pivot, p2 - ending point
            if (Math.Abs(direction.x) > 0.5f) {
                // direction is x
                int way = ((direction.x >= 0) ? 1 : -1);

                p0 = new Vector3(driver_coords.x, y, next_intersection.z - delta * way);

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
                int way = ((direction.z >= 0) ? 1 : -1);

                p0 = new Vector3(next_intersection.x + delta * way, y, driver_coords.z);

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
            
            // setup for line drawing animation 
            origin = p0;
            destination = p1;
            lineRenderer.SetPosition(0, origin);

            drawing = true;
            counter = 0;
            pos = 1;
        }

        // update beginning of line to follow behind motorcycle position       
        direction = p1 - p0;
        direction.Normalize();

        if (Math.Abs(direction.x) > 0.5f)
            p0.x = driver_coords.x;
        else if (Math.Abs(direction.z) > 0.5f)
            p0.z = driver_coords.z;
        lineRenderer.SetPosition(0, p0);
        
        // update origin and destination for animation purposes
        if (counter >= 1 && lineRenderer.positionCount == 2) {
            origin = p1;
            destination = p2;
            counter = 0;
            pos = 2;
            lineRenderer.positionCount = 3;
        }
        
        // draw line
        counter += 0.1f;
        lineRenderer.SetPosition(pos, Vector3.Lerp(origin, destination, counter));
    }

    private void ResetDirectionLine()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, new Vector3(1000,1000,1000));
        lineRenderer.SetPosition(1, new Vector3(1000, 1000, 1000));
        drawing = false;
    }

    private void UpdateLineColor()
    {
        if (curColor.Equals("white"))
            lineRenderer.endColor = Color.white;
        else if (curColor.Equals("green"))
            lineRenderer.endColor = Color.green;
    }

    private void WriteInfo(float time)
    {
        string path = "Assets/Resources/routeinfo.txt";
        StreamWriter writer = new StreamWriter(path, true);
        string info = curRoute.ToString() + "\t" + time.ToString();

        writer.WriteLine(info);
        writer.Close();
    }
}


