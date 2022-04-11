using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int width = 4;
    [SerializeField] private int height = 4;
    [SerializeField] private NodeManager nodeManager;
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private SpriteRenderer boardPrefab;
    [SerializeField] private List<BlockType> blockType;
    [SerializeField] private float travelTime = 0.2f;
    [SerializeField] private int winCondition = 2048;
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private int score = 0;


    private List<NodeManager> nodeList;
    private List<BlockManager> blockList;
    private GameState gameState;
    private int round;

    private BlockType GetBlockTypeByValue(int value) => blockType.First(t => t.Value == value);

    void Start()
    {        
        ChangeGameState(GameState.GenerateLevel);
    }

    private void ChangeGameState(GameState newGameState)
    {
        gameState = newGameState;

        switch(newGameState)
        {
            case GameState.GenerateLevel:
                GenerateGrid();
                break;
            case GameState.SpawningBlocks:
                SpawnBlocks(round++ == 0 ? 2 : 1);                
                break;
            case GameState.WaitingInput:
                break;
            case GameState.Moving:
                break;
            case GameState.Win:
               winScreen.SetActive(true);
                //Invoke(nameof(DelayedWinScreenText), 1.5f);
                break;
            case GameState.Lose:
                loseScreen.SetActive(true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newGameState), newGameState, null); 
        }
    }

    void Update()
    {
        if (gameState != GameState.WaitingInput) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) Shift(Vector2.left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) Shift(Vector2.right);
        if (Input.GetKeyDown(KeyCode.UpArrow)) Shift(Vector2.up);
        if (Input.GetKeyDown(KeyCode.DownArrow)) Shift(Vector2.down);
    }
    private void GenerateGrid()
    {
        round = 0;
        nodeList = new List<NodeManager>();
        blockList = new List<BlockManager>();
        for (int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                var node = Instantiate(nodeManager,new Vector2(x, y), Quaternion.identity);
                nodeList.Add(node);
            }
        }

        var center = new Vector2((float) width / 2 - 0.5f, (float) height / 2 - 0.5f);
        var board = Instantiate(boardPrefab, center, Quaternion.identity);

        board.size = new Vector2(width, height);

        Camera.main.transform.position = new Vector3(center.x, center.y, -10);

        ChangeGameState(GameState.SpawningBlocks);
    }

    private void SpawnBlocks(int amount)
    {
        var freeNodes = nodeList.Where(n => n.OccupiedBlock == null).OrderBy(b=> Random.value).ToList();
        foreach(var node in freeNodes.Take(amount))
        {
            SpawnBlock(node, Random.value > 0.8f ? 4 : 2);
        }

        if (freeNodes.Count() == 0 && !blockManager.CanMerge(blockManager.Value))
        {            
            Debug.Log("not possible");
            // You lost the game
            ChangeGameState(GameState.Lose);
            return;
        }

        ChangeGameState(blockList.Any(b => b.Value == winCondition) ? GameState.Win : GameState.WaitingInput);
    }


    void SpawnBlock(NodeManager node, int value)
    {
        var block = Instantiate(blockManager, node.pos, Quaternion.identity);
        block.Initialize(GetBlockTypeByValue(value));
        block.SetBlock(node);
        blockList.Add(block);
    }

    void Shift(Vector2 dir)
    {
        ChangeGameState(GameState.Moving);

        var orderedBlocks = blockList.OrderBy(b => b.pos.x).ThenBy(b => b.pos.y).ToList();
        if (dir == Vector2.right || dir == Vector2.up) orderedBlocks.Reverse();

        foreach (var block in orderedBlocks)
        {
            var next = block.Node;
            do
            {
                block.SetBlock(next);

                var possibleNode = GetNodeAtPosition(next.pos + dir);
                if (possibleNode != null)
                {
                    // We know a node is present
                    // If it's possible to merge, set merge
                    if (possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value))
                    {                        
                        block.MergeBlock(possibleNode.OccupiedBlock);                        
                    }
                    // Otherwise, can we move to this spot?
                    else if (possibleNode.OccupiedBlock == null)
                    {
                        next = possibleNode;
                    }                              
                    // None hit? End do while loop
                }

            } while (next != block.Node);

            block.transform.DOMove(block.Node.pos, travelTime);
        }

        var sequence = DOTween.Sequence();

        foreach (var block in orderedBlocks)
        {
            var movePoint = block.MergingBlock != null ? block.MergingBlock.Node.pos : block.Node.pos;

            sequence.Insert(0, block.transform.DOMove(movePoint, travelTime).SetEase(Ease.InQuad));
        }

        sequence.OnComplete(() => {
            foreach (var block in orderedBlocks.Where(b => b.MergingBlock != null))
            {
                MergeBlocks(block.MergingBlock, block);
            }

            ChangeGameState(GameState.SpawningBlocks);
        } );
    }

    void MergeBlocks(BlockManager baseBlock, BlockManager mergingBlock)
    {
        SpawnBlock(baseBlock.Node, baseBlock.Value * 2);
        
        score += baseBlock.Value * 2;        
        scoreText.text = "Score:" + score;    

        DestroyingBlock(baseBlock);
        DestroyingBlock(mergingBlock);
    }

    void DestroyingBlock(BlockManager block)
    {
        blockList.Remove(block);
        Destroy(block.gameObject);
    }

    NodeManager GetNodeAtPosition(Vector2 pos)
    {
        return nodeList.FirstOrDefault(n => n.pos == pos);
    }
}

[Serializable] 
public struct BlockType
{
    public int Value;
    public Color Color;
}

public enum GameState
{
    GenerateLevel,
    SpawningBlocks,
    WaitingInput,
    Moving,
    Win,
    Lose
}
