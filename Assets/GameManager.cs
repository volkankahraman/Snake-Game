using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SG
{
    public class GameManager : MonoBehaviour
    {
        public int maxHeight = 15;
        public int maxWidth = 17;

        int currentScore;
        int highScore;

        public Color colorodd;
        public Color coloreven;
        public Color playerClr;
        public Color foodClr = Color.red;

        public Text currentScoreTxt;
        public Text highScoreTxt;
       
        GameObject mapObject;
        GameObject playerObj;
        GameObject tailParent;
        GameObject foodObj;

        public Transform camHolder;

        SpriteRenderer mapRenderer;
        SpriteRenderer playerRenderer;

        Sprite playerSprite;

        Node playerNode;
        Node foodNode;

        Direction curDirection;
        Direction prevDirection;

        List<Node> availableNodes = new List<Node>();
        List<SpecialNode> tailNodes = new List<SpecialNode>();

        Node[,] Grid;

        public UnityEvent onStart;
        public UnityEvent onGameOver;
        public UnityEvent onEat;

        private void Start()
        {
            onStart.Invoke();
            Time.timeScale = 0;

        }

        public void StartGame()
        {
            Time.timeScale = 1;
        }

        public void StartNewGame()
        {
            currentScore = 0;
            Time.timeScale = 1;
            ClearGame();
            GenerateMap();
            CratePlayer();
            PlaceCamera();
            CrateFood();
            UpdateScore();
            curDirection = Direction.right;
        }
        void ClearGame()
        {
            if (mapObject != null)
                Destroy(mapObject);
            if (tailParent != null)
                Destroy(tailParent);
            if (playerObj != null)
                Destroy(playerObj);
   
                Destroy(foodObj);
            foreach (var t in tailNodes)
            {
                if (t.gameObj != null)
                    Destroy(t.gameObj);
            }
            tailNodes.Clear();
            availableNodes.Clear();
            Grid = null;
        }

        void GenerateMap()
        {
            mapObject = new GameObject("Map");
            mapRenderer = mapObject.AddComponent<SpriteRenderer>();

            Texture2D txtMap = new Texture2D(maxWidth, maxHeight);

            Grid = new Node[maxWidth, maxHeight];


            for (int i = 0; i < maxWidth; i++)
            {
                for (int j = 0; j < maxHeight; j++)
                {
                    Vector3 worldPstn = Vector3.zero;
                    worldPstn.x = i;
                    worldPstn.y = j;

                    Node n = new Node
                    {
                        x = i,
                        y = j,
                        worldPosition = worldPstn
                    };

                    Grid[i, j] = n;

                    availableNodes.Add(n);

                    #region Görsel
                    if ((i % 2) == 0)
                    {
                        if ((j % 2) == 0)
                        {
                            txtMap.SetPixel(i, j, colorodd);
                        }
                        else
                        {
                            txtMap.SetPixel(i, j, coloreven);
                        }
                    }
                    else
                    {
                        if ((j % 2) == 0)
                        {
                            txtMap.SetPixel(i, j, coloreven);
                        }
                        else
                        {
                            txtMap.SetPixel(i, j, colorodd);
                        }
                    }
                    #endregion
                }
            }
            txtMap.filterMode = FilterMode.Point;
            txtMap.Apply();
            Rect rect = new Rect(0, 0, maxWidth, maxHeight);
            Camera.main.orthographicSize = 8;
            Sprite sprite = Sprite.Create(txtMap, rect, Vector2.zero, 1, 0, SpriteMeshType.FullRect);
            mapRenderer.sprite = sprite;
        }

        void CratePlayer()
        {
            playerObj = new GameObject("Player");
            playerRenderer = playerObj.AddComponent<SpriteRenderer>();
            playerSprite = CreateSprite(playerClr);
            playerRenderer.sprite = playerSprite;
            playerObj.transform.localScale = Vector3.one * 1.2f;
            playerRenderer.sortingOrder = 1;
            playerNode = GetNode(4, 5);


            PlacePlayerNode(playerObj, playerNode.worldPosition);

            tailParent = new GameObject("tailParent");
        }

        void CrateFood()
        {
            foodObj = new GameObject("Food");
            SpriteRenderer foodRenderer = foodObj.AddComponent<SpriteRenderer>();
            foodRenderer.sprite = CreateSprite(foodClr);
            foodRenderer.sortingOrder = 1;
            RandomlyPlaceFood();
        }

        void PlaceCamera()
        {
            Node n = GetNode(maxWidth/2, maxHeight/2);
            Vector3 p = n.worldPosition + Vector3.one * 0.5f;
            camHolder.position = p;

        }


        public float moveRate = 0.1f;
        public float timer;

        bool up, down, left, right;


        public enum Direction
        {
            up,down,left,right
        }

        void GetInput()
        {
            up = Input.GetButtonDown("Up");

            down = Input.GetButtonDown("Down");

            left = Input.GetButtonDown("Left");

            right = Input.GetButtonDown("Right");
        }

        void SetDirection()
        {
            if (up)
            {
                if(!isOpposite(Direction.up))
                    curDirection = Direction.up;
            }
            else if (down)
            {
                if (!isOpposite(Direction.down))
                    curDirection = Direction.down;
            }
            else if (left)
            {
                if (!isOpposite(Direction.left))
                    curDirection = Direction.left;
            }
            else if (right)
            {
                if (!isOpposite(Direction.right))
                    curDirection = Direction.right;
            }
        }

        void MovePlayer()
        {
            int x = 0;
            int y = 0;

            switch (curDirection) {
                case Direction.up:
                    y = 1;
                    break;
                case Direction.down:
                    y = -1;
                    break;
                case Direction.left:
                    x = -1;
                    break;
                case Direction.right:
                    x = 1;
                    break;
            }
            Node targetNode = GetNode(playerNode.x + x, playerNode.y + y);

            if(targetNode == null)
            {
                //Kaybettin
                onGameOver.Invoke();
                Time.timeScale = 0;


            }
            else
            {
                if (isTailNode(targetNode))
                {
                    //Kaybettin 
                    onGameOver.Invoke();
                    Time.timeScale = 0;


                }

                bool isEaten = false;

                if(targetNode == foodNode)
                {
                    isEaten = true;
                }

                Node prevNode = playerNode;
                moveTail();

                availableNodes.Remove(playerNode);
                PlacePlayerNode(playerObj, targetNode.worldPosition);
                playerNode = targetNode;
                availableNodes.Add(prevNode);

                //Kuyruğu oynat

                if (isEaten)
                {
                    currentScore++;
                    if (currentScore >= highScore)
                    {
                        highScore = currentScore;
                    }
                    onEat.Invoke();

                    tailNodes.Add(CreateTailNode(prevNode.x,prevNode.y));
                    availableNodes.Remove(prevNode);
                    //Yılanı büyüt
                    if (availableNodes.Count>0)
                    {
                        RandomlyPlaceFood();
                        // Kuyruğu büyüt
                    }
                    else
                    {
                        //Oyunu kazandın
                    }
                }

            }
        }
        
        
        void Update()
        {
            GetInput();
            SetDirection();

            timer += Time.deltaTime;
            if (timer > moveRate)
            {
                MovePlayer();
                timer = 0;
            }
        }

        void moveTail()
        {
            Node prevNode = null;
            for (int i = 0; i < tailNodes.Count; i++)
            {
                SpecialNode sNode = tailNodes[i];

                availableNodes.Add(sNode.node);

                if (i==0)
                {
                    prevNode = sNode.node;
                    sNode.node = playerNode;
                }
                else
                {
                    Node prev = sNode.node;
                    sNode.node = prevNode;
                    prevNode = prev;
                }
                availableNodes.Remove(sNode.node);
                PlacePlayerNode(sNode.gameObj, sNode.node.worldPosition);

            }
        }

        void PlacePlayerNode(GameObject obj,Vector3 pos)
        {
            pos += Vector3.one * 0.5f;
            obj.transform.position = pos;
        }
        
        public void UpdateScore()
        {
            currentScoreTxt.text = "Skor :"+currentScore.ToString();
            highScoreTxt.text = "Yüksek Skor : "+highScore.ToString();
        }

        SpecialNode CreateTailNode(int x, int y)
        {
            SpecialNode s = new SpecialNode();
            s.node = GetNode(x,y);
            s.gameObj = new GameObject();
            s.gameObj.transform.parent = tailParent.transform;
            s.gameObj.transform.position = s.node.worldPosition;
            s.gameObj.transform.localScale = Vector3.one * 0.95f;
            SpriteRenderer tailRenderer = s.gameObj.AddComponent<SpriteRenderer>();
            tailRenderer.sprite = playerSprite;
            tailRenderer.sortingOrder = 1;
            return s;
        }

        Node GetNode(int x, int y)
        {
            if (x >= maxWidth || x < 0 || y >= maxHeight || y < 0)
            {
                return null;
            }
            {
                return Grid[x, y];
            }
        }

        bool isOpposite(Direction dir)
        {
            switch (dir)
            {
                default:
                case Direction.up:
                    if (curDirection == Direction.down)
                        return true;
                    else
                        return false;
                case Direction.down:
                    if (curDirection == Direction.up)
                        return true;
                    else
                        return false;
                case Direction.left:
                    if (curDirection == Direction.right)
                        return true;
                    else
                        return false;
                case Direction.right:
                    if (curDirection == Direction.left)
                        return true;
                    else
                        return false;
            }
        }

        bool isTailNode(Node tNode)
        {
            for (int i = 0; i < tailNodes.Count; i++)
            {
                if (tNode == tailNodes[i].node)
                {
                    return true;
                }
            }
            return false;
        }
        
        void RandomlyPlaceFood()
        {
            int rand = Random.Range(0,availableNodes.Count);
            Node n = availableNodes[rand];
            PlacePlayerNode(foodObj, n.worldPosition);
            foodNode = n;
        }

        Sprite CreateSprite(Color targetColor)
        {
            Texture2D txtSprite = new Texture2D(1, 1);
            txtSprite.SetPixel(0, 0, targetColor);
            txtSprite.filterMode = FilterMode.Point;
            txtSprite.Apply();
            Rect rect = new Rect(0, 0, 1, 1);
            return Sprite.Create(txtSprite, rect, Vector2.one * 0.5f, 1, 0, SpriteMeshType.FullRect);
        }


    }
}