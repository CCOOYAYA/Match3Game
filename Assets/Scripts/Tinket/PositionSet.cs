using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionSet : MonoBehaviour
{
    [SerializeField] Transform[] posList;

    public Vector3 Pos(int index) => (index < posList.Length) ? posList[index].position : transform.position;
    public Transform PosTransform(int index) => (index < posList.Length) ? posList[index] : transform;
    public int PosCount => posList.Length;

    /*
    private void Start()
    {
        gameObject.SetActive(false);
    }
    */
}
