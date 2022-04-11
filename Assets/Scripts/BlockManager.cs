using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public int Value;
    public Color color;
    public NodeManager Node;
    public BlockManager MergingBlock;
    public bool Merging;
    public Vector2 pos => transform.position;
    
    [SerializeField] private SpriteRenderer block_spriteRenderer;
    [SerializeField] private TextMeshPro textmeshPro;

    public void Initialize(BlockType type)
    {
        Value = type.Value;
        color = type.Color;
        block_spriteRenderer.color = type.Color;
        textmeshPro.text = type.Value.ToString();
    }

    public void SetBlock(NodeManager node)
    {
        if (Node != null) Node.OccupiedBlock = null;
        Node = node;
        Node.OccupiedBlock = this;
    }

    public void MergeBlock(BlockManager blockToMergeWithAnotherBlock)
    {
        // Set the block we are merging with
        MergingBlock = blockToMergeWithAnotherBlock;

        // Set current node as unoccupied to allow blocks to use it
        Node.OccupiedBlock = null;

        // Set the base block as merging, so it does not get used twice.
        blockToMergeWithAnotherBlock.Merging = true;
    }

    public bool CanMerge(int value) => value == Value && !Merging && MergingBlock == null;
}
