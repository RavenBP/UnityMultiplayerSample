using UnityEngine;

public class NetworkPlayer : MonoBehaviour
{
    [SerializeField]
    private float speed = 5.0f;

    private bool isPlayer = false;
    public string netId = "";
    private Vector3 position;
    private Vector3 rotation;
    public NetworkClient networkClient;

    private void Awake()
    {
        networkClient = FindObjectOfType<NetworkClient>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (isPlayer == true)
        {
            InvokeRepeating("UpdatePosition", 1, 0.03f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlayer == true)
        {
            position = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0.0f) * speed * Time.deltaTime;
            transform.Translate(position);
        }
    }

    public void SetPlayer(string id, Vector3 position, Vector3 rotation, Color color)
    {
        netId = id;

        if (networkClient.clientId == id)
        {
            isPlayer = true;
        }
 
        this.position = position;
        this.rotation = rotation;
        this.GetComponent<Renderer>().material.color = new Color(color.r, color.g, color.b);
    }

    public void UpdatePosition()
    {
        networkClient.UpdatePlayer(netId, position, rotation);
    }
}
