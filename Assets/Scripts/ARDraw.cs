using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Newtonsoft.Json;
using Firebase.Extensions;
using System.Text;
using System.IO;




[System.Serializable]
public class DoodleInfo
{
    public Dictionary<string, List<float[]>> vectors = new Dictionary<string, List<float[]>>();

}





public class ARDraw : MonoBehaviour
{


    Camera arCamera;


    Vector3 anchor = new Vector3(0, 0, 0.3f);



    bool anchorUpdate = false;
    //should anchor update or not
    //
    public GameObject linePrefab;
    //prefab which genrate the line for user

    LineRenderer lineRenderer;
    //LineRenderer which connects and generate

    public List<LineRenderer> lineList = new List<LineRenderer>();

    public string uniqueName = "newname";

    public DoodleInfo doodleinfo = new DoodleInfo();


    //List of lines drawn

    List<float[]> tem = new List<float[]>();
    public Transform linePool;

    int counter = 0;

    public bool use;
    //code is in use or not


    public bool startLine;
    private Touch touch;

    //already started line or not


    void Start()
    {
        arCamera = GameObject.Find("AR Camera").GetComponent<Camera>();
    }

    void Update()
    {
        if (use)
        {
            if (startLine)
            {
                Debug.Log("Update");
                UpdateAnchor();
                DrawLinewContinue();
            }
        }
    }



    void UpdateAnchor()
    {
        if (anchorUpdate)
        {
            Vector3 temp = Input.mousePosition;
            Debug.Log("UpdateAnchor");
            temp.z = 0.3f;
            anchor = arCamera.ScreenToWorldPoint(temp);

            float[] tmp = { anchor.x, anchor.y, anchor.z };
            tem.Add(tmp);

            //tem.Clear();

        }


    }

    public void MakeLineRenderer()
    {
        GameObject tempLine = Instantiate(linePrefab);
        Debug.Log("MakeLine");
        tempLine.transform.SetParent(linePool);
        tempLine.transform.position = Vector3.zero;
        tempLine.transform.localScale = new Vector3(1, 1, 1);

        anchorUpdate = true;
        UpdateAnchor();

        lineRenderer = tempLine.GetComponent<LineRenderer>();
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, anchor);

        startLine = true;
        lineList.Add(lineRenderer);
    }

    public void DrawLinewContinue()
    {
        lineRenderer.positionCount = lineRenderer.positionCount + 1;
        //Debug.Log("DrawLine");
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, anchor);
    }

    public void StartDrawLine()
    {
        use = true;
        Debug.Log("StartDrawLine");

        if (!startLine)
        {
            MakeLineRenderer();
        }
    }

    public void StopDrawLine()
    {

        
        use = false;
        startLine = false;
        lineRenderer = null;
        anchorUpdate = false;
        doodleinfo.vectors.Add(counter.ToString(), new List<float[]>(tem));
        counter++;
        tem.Clear();
        Debug.Log("Counter " + counter.ToString());
        Debug.Log("List " + tem.Count);

    }

    public void Undo()
    {
        LineRenderer undo = lineList[lineList.Count - 1];
        Destroy(undo.gameObject);
        lineList.RemoveAt(lineList.Count - 1);
    }

    public void ClearScreen()
    {
        foreach (LineRenderer item in lineList)
        {
            Destroy(item.gameObject);
        }
        lineList.Clear();
    }


    public void uploadData()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;


        DocumentReference docRef = db.Collection("anchors").Document("ARDoodle");

        StringBuilder sb = new StringBuilder();
        StringWriter sw = new StringWriter(sb);
        counter = 0;

        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            var serializer = new JsonSerializer();
            serializer.Serialize(writer, doodleinfo);
        }
        Debug.Log("ad" + sb.ToString());


        Dictionary<string, string> anchors = new Dictionary<string, string>{
             {"vectorPoints", sb.ToString()}
         };

        docRef.SetAsync(anchors).ContinueWithOnMainThread(task =>
          {
              Debug.Log(
                      "NOICE "
                      + "the users collection.");
          });
          doodleinfo.vectors.Clear();
    }
    public void newLines()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("anchors").Document("ARDoodle");
        List<List<float>> tmpVectors = new List<List<float>>();
        List<Vector3> vectors = new List<Vector3>();


        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            DocumentSnapshot snapshot = task.Result;

            if (snapshot.Exists)

            {
                Dictionary<string, List<float>> value;
                snapshot.TryGetValue("vectorPoints", out value);

                int count = value.Count;

                for (int i = 0; i < count; i++)
                {
                    tmpVectors.Add(value[i.ToString()]);
                }
                for (int i = 0; i < tmpVectors.Count; i++)
                {
                    Vector3 tempVector = new Vector3(tmpVectors[i][0], tmpVectors[i][1], tmpVectors[i][2]);
                    vectors.Add(tempVector);
                }

                GameObject tempLine = Instantiate(linePrefab);
                tempLine.transform.SetParent(linePool);
                tempLine.transform.position = Vector3.zero;
                tempLine.transform.localScale = new Vector3(1, 1, 1);
                lineRenderer = tempLine.GetComponent<LineRenderer>();
                lineRenderer.positionCount = 1;
                lineRenderer.SetPosition(0, vectors[0]);
                for (int i = 1; i < vectors.Count; i++)
                {
                    Debug.Log("NOICE it runs 4" + i);
                    lineRenderer.positionCount = lineRenderer.positionCount + 1;
                    lineRenderer.SetPosition(lineRenderer.positionCount - 1, vectors[i]);
                }




            }
            else
            {
                Debug.Log("Document does not exist!");
            }
        });
    }


}
