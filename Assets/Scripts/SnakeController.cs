/*
 * Author:      Matthew Brown
 * Date:        11/19/24
 * Description: This is the game controller for the snake game. It makes the snake move forward constantly.
 *              The snake can only turn 90 degrees from the direction of travel.
 *              It spawns berries randomly on the screen for the snake to pickup. These berries are spawned where the snake isn't.
 *              When berries are picked up the snakes length increases.
 *              If the player hits a wall or the snakes tail game over.
 *              The objective is to get as many berries as possible.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SnakeController : MonoBehaviour
{
    //Game Logic
    private float timer = 0; //Timer for snake to move every .2f seconds
    private bool gameRunning = true; //Flag to tell the script the game is still running
    private int berryCount = 0; //How many berries have been eaten
    //Snake
    private List<Transform> snake; //List of transforms for head and tail objects of the snake
    public Transform head; //Unity link for head prefab
    public Transform tail; //Unity link for tail prefab
    //Movement
    private float xMove = 0; //Used to move the snake in x direction
    private float zMove = 1; //Used to move the snake in z directiono   
    private float tempX = 0; //Buffer to hold if left or right was clicked inbetween each movement
    private float tempZ = 0; //Buffer to hold if up or down was clicked inbetween each movement
    //Berry info
    public GameObject Berry; //Unity link for berry prefab
    private GameObject currentBerry; //Holds the object for the berry currently spawned

    private AudioSource[] headSounds; //Holds all sounds from the head
    //UI stuff
    public Text gameOver; //Unity link for displaying game over text to ui
    public Text berryCountText; //Unity link for displaying the berry count to ui
    public Button restartButton; //Unity link for displaying the restart button

    // Start is called before the first frame update
    void Start()
    {
        berryCountText.text = berryCount.ToString();
        gameOver.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);

        snake = new List<Transform> //Instantiate a snake of length 5
        {
            Instantiate(head, new Vector3(10, 0, 10), Quaternion.identity),
            Instantiate(tail, new Vector3(10, 0, 9), Quaternion.identity),
            Instantiate(tail, new Vector3(10, 0, 8), Quaternion.identity),
            Instantiate(tail, new Vector3(10, 0, 7), Quaternion.identity),
            Instantiate(tail, new Vector3(10, 0, 6), Quaternion.identity)
        };

        headSounds = snake[0].GetComponents<AudioSource>();
        spawnBerry(); //Intial berry spawn
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        if(x != 0) //Hold a buffer if left or right is ever clicked
        {
            tempX = x;
        }
        if(z != 0) //Hold a buffer if up or down is clicked
        {
            tempZ = z;
        }

        if (timer >= 0.2f) //Every .2 seconds move the snake
        {
            moveSnake();
            timer = 0;
        }
        timer += Time.deltaTime;
    }

    /*
     * Moves the snake forwards always. Snake can only turn 90 degrees.
     * Detects if any collisions have been made with a berry, walls, or itself
     */
    void moveSnake()
    {
        if(zMove == 1 || zMove == -1) //If we are moving in the z direction
        {
            if(tempX != 0) //If left or right have been clicked s
            {
                xMove = tempX;
                zMove = 0;
            }
        }
        else //We must be moving in the x direction
        {
            if(tempZ != 0) //if up or down has been clicked
            {
                zMove = tempZ;
                xMove = 0;
            }
        }
        //Clear here so if both are clicked in the timer period before this is called we don't go the other way next call
        tempZ = 0; 
        tempX = 0;

        if (detectCollision() && gameRunning) //Determine if we have collided with wall or tail
        {
            gameRunning = false;
            headSounds[1].Play();
            gameOver.gameObject.SetActive(true);
            restartButton.gameObject.SetActive(true);
        } 
        if (gameRunning)
        {
            //Move the snake
            for (int i = snake.Count-1; i>= 1; i-- ) 
            {
                snake[i].position = snake[i-1].position;
            }
            snake[0].Translate(new Vector3(xMove, 0, zMove));
            checkBerry();
        }
    }
    /*
     * Function determines if the head of the snake has hit either the wall or its own tail
     * Returns true if it struct the tail or wall, false otherwise
     */
    bool detectCollision()
    {
        // If the border was hit
        float headX = snake[0].position.x + xMove;
        float headZ = snake[0].position.z + zMove;
        if((headX >= 20 || headZ >= 20) || (headX < 0 || headZ < 0))
        {
            return true;
        }

        //If we hit the tail
        for(int i = 1; i < snake.Count; i++)
        {
            if (snake[i].position.x == headX && snake[i].position.z == headZ)
            {
                return true;
            }
        }
        
        return false;
    }
    /*
     * Checks if the snake is currently over a berry to eat
     * If a berry is below the head the berry is destroyed and another is spawned
     * 3 tail pieces are added to the end of the snake
     * Counts how many berries have been eaten in total
     */
    void checkBerry()
    {
        Vector3 berryPos = currentBerry.transform.position;
        if (berryPos.x == snake[0].position.x && berryPos.z == snake[0].position.z) //If the head is on the berry
        {
            Destroy(currentBerry);
            currentBerry = null;

            spawnBerry();

            Vector3 endTailPos = snake[snake.Count - 1].position; //Get end tail position  to spawn more tail positions
            for (int i = 0; i < 3; i++)
            {
                snake.Add(Instantiate(tail, endTailPos, Quaternion.identity));
            }

            headSounds[0].Play();
            berryCount++;
            berryCountText.text = berryCount.ToString();
        }
    }


    /*
     * Spawn a berry in a position not occupied by the snake 
     */
    void spawnBerry()
    {
        List<Vector3> validSpawnList = getValidSpawn();

        validSpawnList = shuffle(validSpawnList);

        Vector3 validSpawnPoint = getFinalSpawnPoint(validSpawnList);
        
        validSpawnPoint.y = .5f; //Raise berry out of the floor
        currentBerry = Instantiate(Berry, validSpawnPoint, Quaternion.identity);
    }

    /*
     * Passed a randomized list of available berry spawn points.
     * Picks the first position in the list that is 5 units away or the farthest distance point if there is not one 5 units away.
     * Returns the vector for where to spawn the berry.
     */
    Vector3 getFinalSpawnPoint(List<Vector3> spawnList)
    {
        Vector3 farthestPoint = Vector3.zero;
        float farthestDistance = 0;

        foreach (Vector3 point in spawnList)
        {
            float distance = Vector3.Distance(point, snake[0].position);
            if (distance >= 5f)
            {
                return point;
            }
            if(distance > farthestDistance) //If there is no point at least 5 away
            {
                farthestDistance = distance;
                farthestPoint = point;
            }
        }
        return farthestPoint;
    }

    /*
     * Creates a grid of positions that have a piece of the snake in them.
     * Then it creates a list of points not occupied by the snake.
     * Returns the list of valid spawn points.
     */
    List<Vector3> getValidSpawn()
    {
        //Make every location occupied by the snake not spawnable
        bool[,] currentMapGrid = new bool[20, 20];
        for (int i = 0; i < snake.Count; i++)
        {
            currentMapGrid[(int)snake[i].position.x, (int)snake[i].position.z] = true;
        }

        //Make a list of valid spawn locations
        List<Vector3> validSpawn = new List<Vector3>();
        for (int i = 0; i < 20; i++)
        {
            for (int j = 0; j < 20; j++)
            {
                if (!currentMapGrid[i, j])
                {
                    validSpawn.Add(new Vector3(i, 0, j));
                }
            }
        }
        return validSpawn;
    }

    /*
     * Implementation of the Fisher-Yates Shuffle
     * Shuffles the list to have a random order.
     */
    List<T> shuffle<T>(List<T> list)
    {

        for(int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
        return list;
    }
    
    /*
     * Function for reset button click in unity to reload the scene
     */
    public void resetClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
