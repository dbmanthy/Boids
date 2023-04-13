using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Thank you https://www.youtube.com/watch?v=rQG9aUWarwE&ab_channel=SebastianLague
public class FieldOfView : MonoBehaviour
{
    [Range(0, 360)]
    public float viewAngle;
    public float viewRadius;
    public float meshResolution;
    public float edgeDistThreashold;
    public int edgeResolution;

    public LayerMask boidMask;
    public LayerMask obsticleMask;

    [HideInInspector]
    public List<Transform> visibleTargets = new List<Transform>();
    public MeshFilter viewMeshFilter;

    Mesh viewMesh;

    void Start()
    {
        StartCoroutine("FindTargetsWithDelay", .2f);
        viewMesh = new Mesh();
        viewMesh.name = "ViewMesh";
        viewMeshFilter.mesh = viewMesh;
    }

    void LateUpdate()
    {
        DrawFieldOfView();
    }

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while(true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibileTargets();
        }
    }

    void DrawFieldOfView()
    {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;
        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo lastViewCast = new ViewCastInfo();

        for(int i = 0; i < stepCount; i++)
        {
            float angle = transform.eulerAngles.z - viewAngle / 2 + stepAngleSize * i;
            //Debug.DrawLine(transform.position, transform.position + DirFromAngle(angle, true) * viewRadius, Color.red);
            ViewCastInfo viewCast = ViewCast(angle);

            if(i > 0) //have to wait unitl lastViewCastInfo is set
            {
                bool edgeDstThreasholdExceeded = Mathf.Abs(lastViewCast.dist - viewCast.dist) > edgeDistThreashold;
                if(lastViewCast.hit != viewCast.hit || (lastViewCast.hit && viewCast.hit && edgeDstThreasholdExceeded))
                {
                    EdgeInfo edge = FindEdge(lastViewCast, viewCast);
                    //if points have been altered from default state
                    if(edge.pointA != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointB);
                    }
                }
            }

            viewPoints.Add(viewCast.point);
            lastViewCast = viewCast;
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        //viewmesh is child of thiss so all vertcies need to be in local space
        //transform.position (global space) == Vector3.zero (local space)
        vertices[0] = Vector3.zero;
        for(int i = 0; i < vertexCount-1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                //order dictates which way triangle is rendered (clockwise)
                triangles[(i * 3) + 1] = i + 2;
                triangles[(i * 3) + 2] = i + 1;
            }
        }
        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    void FindVisibileTargets()
    {
        visibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, boidMask);

        for (int i=0; i<targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            //check if target is within view anlge
            if(Vector3.Angle(transform.up, dirToTarget) < viewAngle/2)
            {
                float distToTargetBoid = Vector3.Distance(transform.position, target.position);
                //check no obsticle is between
                if(!Physics.Raycast(transform.position, dirToTarget, distToTargetBoid, obsticleMask))
                {
                    visibleTargets.Add(target);
                }
            }
        }
    }

    public Vector3 DirFromAngle(float angleInDeg, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            //add the objects own rotation to it
            angleInDeg += transform.eulerAngles.z;
        }
        //note::in unity unit circle is defined differently => 90-x converts from unity trig to math trig => sin(90-x)=cos(x) => in unity swap sin and cos
        return new Vector3(-Mathf.Sin(angleInDeg * Mathf.Deg2Rad), Mathf.Cos(angleInDeg * Mathf.Deg2Rad), 0f);
    }

    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit hit;
        if (Physics.Raycast(transform.position, dir, out hit, viewRadius, obsticleMask))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        return new ViewCastInfo(false, transform.position + dir * viewRadius, viewRadius, globalAngle);
    }

    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for(int i = 0; i < edgeResolution; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);

            bool edgeDstThreasholdExceeded = Mathf.Abs(minViewCast.dist - newViewCast.dist) > edgeDistThreashold;


            if (newViewCast.hit == minViewCast.hit && !edgeDstThreasholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }
        return new EdgeInfo(minPoint, maxPoint);
    }

    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dist;
        public float angle;

        public ViewCastInfo(bool hit_, Vector3 point_, float dist_, float angle_)
        {
            hit = hit_;
            point = point_;
            dist = dist_;
            angle = angle_;
        }
    }

    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 pointA_, Vector3 pointB_)
        {
            pointA = pointA_;
            pointB = pointB_;
        }
         
    }
}
