using System.IO;
using System;
using UnityEngine;

public class Results : MonoBehaviour
{
    public int noColumns; // 4 for format; 5 for color
    public int totalUsers;
    public int noRoutes;
    public string testType; // format or color

    private string[,] info; // routes per file collumns
    private int userNo;
    private float[,] sums = new float[3, 3]; // routes per data 

    private StreamReader routeReader;

    private StreamReader trackerReader;
    private float xPos = 0;
    private float yPos = 0;

    private int frameCounter = 0;

    // Start is called before the first frame update
    void Start()
    {
        // basic info medium calcs
        BasicInfoCalcs();

        // open tracker file StreamReader for first user
        userNo = 1;
        trackerReader = new StreamReader("Assets/Resources/users/user" + userNo.ToString() + "/tracker.txt", true);
        routeReader = new StreamReader("Assets/Resources/users/user" + userNo.ToString() + "/routeinfo.txt", true);

        Debug.Log("user: " + userNo);
        this.GetComponent<NavigationController>().UpdateRoute(Convert.ToInt32(routeReader.ReadLine()[0].ToString()));
    }

    // Update is called once per frame
    void Update()
    {
        if (frameCounter > 1)
        {
            char[] delimiterChars = { ' ', '\t' };
            string fileLine = trackerReader.ReadLine();

            //Debug.Log("line read: " + fileLine);

            if (string.IsNullOrEmpty(fileLine))
            {
                trackerReader.Close();
                routeReader.Close();

                userNo++;
                trackerReader = new StreamReader("Assets/Resources/users/user" + userNo.ToString() + "/tracker.txt", true);
                Debug.Log("user: " + userNo);

                routeReader = new StreamReader("Assets/Resources/users/user" + userNo.ToString() + "/routeinfo.txt", true);
                this.GetComponent<NavigationController>().UpdateRoute(Convert.ToInt32(routeReader.ReadLine()[0].ToString()));
            }
            else if (fileLine.Equals("-"))
            {
                Debug.Log("found '-' line");
                this.GetComponent<NavigationController>().UpdateRoute(Convert.ToInt32(routeReader.ReadLine()[0].ToString()));
            }
            else
            {
                string[] lineInfo = fileLine.Split(delimiterChars);
                // x-pos    y-pos   delta-time    x-input   y-input   left-brake

                // update driver's position and rotation
                xPos = (float)Convert.ToDouble(lineInfo[0]);
                yPos = (float)Convert.ToDouble(lineInfo[1]);
            }
            frameCounter = 0;
        }
        frameCounter++;
    }

    void OnDestroy()
    {
        // close StreamReader for tracker file 
        trackerReader.Close();
    }

    private void ParseFile(string path)
    {
        StreamReader reader = new StreamReader(path);
        char[] delimiterChars = { ' ', '\t' };

        string line = reader.ReadLine();
        int step = 0;
        while (!string.IsNullOrEmpty(line))
        {
            string[] lineInfo = line.Split(delimiterChars);
            for (int i = 0; i < noColumns; i++)
                info[step, i] = lineInfo[i];
            
            line = reader.ReadLine();
            step++;
        }
        reader.Close();
    }

    private void BasicInfoCalcs()
    {
        // time and error / collision rounds
        info = new string[noRoutes, noColumns];

        // run through all users
        for (int user = 1; user <= totalUsers; user++)
        {
            // parse file
            string path = "Assets/Resources/users/user" + user.ToString() + "/routeinfo.txt";
            ParseFile(path);

            Debug.Log("user " + user.ToString());
            PrintArray(info);
            
            // add time value to each format from parsed info
            for (int r = 0; r < info.GetLength(0); r++)
            {
                int formatNo;
                if (testType.Equals("format"))
                {
                    // figure out format by route#
                    double f = Convert.ToInt32(info[r, 0]) / 2f;
                    formatNo = (int)Math.Ceiling(f);
                }
                else
                    formatNo = info[r, 4].Equals("white") ? 1 : 2;

                for (int i = 0; i < 3; i++)
                {
                    sums[formatNo - 1, i] += (float)Convert.ToDouble(info[r, i + 1]);
                    //Debug.Log((i + 1).ToString() + "\t" + sums[i, 0] + "\t" + sums[i, 1] + "\t" + sums[i, 2]);
                }
            }
        }

        // divide by total users * number of routes for format 
        for (int i = 0; i < 3; i++)
            for (int k = 0; k < 3; k++)
                sums[i, k] = sums[i, k] / (totalUsers * 2);

        // write output file
        StreamWriter outputFile = new StreamWriter("Assets/Resources/output.txt");
        outputFile.WriteLine("format\ttime\terrors\tcollisions");

        for (int i = 0; i < 3; i++)
            outputFile.WriteLine((i+1).ToString() + "\t" + sums[i,0].ToString() + "\t" + sums[i,1].ToString() + "\t" + sums[i,2].ToString());

        outputFile.Close();
    }

    private void PrintArray(string[,] array) 
    {
        int rowLength = array.GetLength(0);
        int colLength = array.GetLength(1);
        string print = "";
        for (int i = 0; i < rowLength; i++)
        {
            for (int j = 0; j < colLength; j++)
            {
                print += array[i, j] + "\t";
            }
            print += "\n";
        }
        Debug.Log(print);
    }

    public Tuple<float, float> InputValues()
    {
        return Tuple.Create(xPos, yPos);
    }

}
