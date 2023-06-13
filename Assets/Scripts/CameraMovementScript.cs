using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class CameraMovementScript : MonoBehaviour
{
    float speed = 0.06f;
    float zoomSpeed = 10.0f;
    float rotationSpeed = 0.1f;

    float maxHeight = 40f;
    float minHeight = 4f;

    Vector2 p1;
    Vector2 p2;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = 0.06f;
            zoomSpeed = 20.0f;
        }
        else
        {
            speed = 0.035f;
            zoomSpeed = 10.0f;
        }

        float y = transform.position.y;

        float hsp = y * speed * Input.GetAxis("Horizontal");
        float vsp = y * speed * Input.GetAxis("Vertical");
        float scrollSp = Mathf.Log(Mathf.Abs(y)) * -zoomSpeed * Input.GetAxis("Mouse ScrollWheel");

        Vector3 verticalMove = new Vector3(0, scrollSp, 0);
        Vector3 lateralMove = hsp * transform.right;
        Vector3 forwardMove = transform.forward;
        forwardMove.y = 0;
        forwardMove.Normalize();
        forwardMove *= vsp;
        Vector3 move = verticalMove + lateralMove + forwardMove;
        Vector3 pos = transform.position + move;
        pos.y = Mathf.Min(maxHeight, Mathf.Max(minHeight, pos.y));
        transform.position = pos;

        getCameraRotation();
    }

    void getCameraRotation()
    {
        if (Input.GetMouseButtonDown(2))  // middle button is pressed
        {
            p1 = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))  // middle button is held down
        {
            p2 = Input.mousePosition;
            float dx = (p2 - p1).x * rotationSpeed;
            float dy = (p2 - p1).y * rotationSpeed;

            //transform.rotation *= Quaternion.Euler(new Vector3(0, dx, 0));
            //transform.GetChild(0).transform.rotation *= Quaternion.Euler(new Vector3(-dy, 0, 0));
            // transform.rotation *= Quaternion.Euler(new Vector3(-dy, dx, 0));
            // Quaternion r = transform.rotation;
            var r = transform.rotation.eulerAngles;
            r.y -= dx;
            r.x += dy;
            transform.rotation = Quaternion.Euler(r);

            //transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(-transform.rotation.y, 0, 0))
            //    * Quaternion.Euler(new Vector3(-dy, dx, 0))
            //    * Quaternion.Euler(new Vector3(transform.rotation.y, 0, 0));

            p1 = p2;
        }
    }
}
