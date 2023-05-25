using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFUI.Core;

namespace WPFUI.MVVM.Model;

public class BlocksModel : ObservableObject
{
    public BlocksModel()
    {
        
    }
    
    public static Dictionary<string,Block> GetAllBlocks()
    {
        var blockList = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        return (blockList);
    }

    public static List<Block> GetAllActiveBlocks()
    {
        var fullBlockList = GetAllBlocks();
        List<string> keyList = fullBlockList.Keys.ToList();
        List<Block> activeBlocks = new List<Block>();
        for (int i = 0; i < keyList.Count; i++)
        {
            Block iBlock = fullBlockList[keyList[i]];
            if (iBlock.Status)
            {
               activeBlocks.Add(iBlock);
            }
        }
        return activeBlocks;
    }

    public static Block CreateNewBlock(string name, string type, List<string> programs)
    {
        /*CHECK IF SELECTED PROGRAM IS IN DATABASE AND IF NOT THEN ADD TO DATABASE*/
        return new Block { Name = name, Status = true, Type = type, Programs = programs, Conditions = new Dictionary<int, BlockCondition>()};
    }
    
    public static void AppendBlockToDatabase(Block block)
    {
        var existingBlocks = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        existingBlocks[block.Name] = block;
        using (StreamWriter writer = new StreamWriter("user\\blocks.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(existingBlocks));
        }
    }

    public static Block CreateNewBlockCondition(string blocktype, Block block, List<string> criteria, List<string> timecriteria)
    {
        List<int> blockConditionKeys = block.Conditions.Keys.ToList();
        int newBlockConditionID;
        if (blockConditionKeys.Count > 0)
        {
            newBlockConditionID = blockConditionKeys.Max() + 1;
        }
        else
        {
            newBlockConditionID = 1;
        }
        BlockCondition blockCondition = new BlockCondition{ Criteria = criteria, TimeCriteria = timecriteria };
        block.Conditions[newBlockConditionID] = blockCondition;
        return block;
    }

    public static void UpdateBlock(string blockName, Block block)
    {
        var existingBlocks = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        existingBlocks[blockName] = block;
        using (StreamWriter writer = new StreamWriter("user\\blocks.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(existingBlocks));
        }
    }

    public static void CheckAllBlocks(string program, int pid)
    {
        var blockList = GetAllBlocks();
        foreach (Block iBlock in blockList.Values)
        {
            if (iBlock.Programs.Contains(program))
            {
                Dictionary<string, object> blockStatus = GetBlockStatus();
                if ((iBlock.Type == "usage-limit-total") || (iBlock.Type == "usage-limit-perapp"))
                {
                    if (blockStatus.ContainsKey(iBlock.Name))
                    {
                        Dictionary<string, int> iBlockDict = ((JObject)blockStatus[iBlock.Name]).ToObject<Dictionary<string, int>>();
                        if (iBlockDict.ContainsKey(program))
                        {
                            iBlockDict[program] = iBlockDict[program] + 1;
                        }
                        else
                        {
                            iBlockDict[program] = 1;
                        }
                        blockStatus[iBlock.Name] = iBlockDict;
                    }
                    else
                    {
                        blockStatus[iBlock.Name] = new Dictionary<string, int>{{program, 1}}; 
                    }
                    WriteBlockStatus(blockStatus);
                }
                /*parse more conditions here later*/
            }
        }
        /* Go through each block again and check if the current program needs to be blocked */
        foreach (Block block in blockList.Values)
        {
            if (block.Status)
            {
                if (block.Programs.Contains(program))
                {
                    Dictionary<string, object> blockStatus = GetBlockStatus();
                    if (block.Type == "usage-limit-total")
                    {
                        Dictionary<string, int> blockDict = ((JObject)blockStatus[block.Name]).ToObject<Dictionary<string, int>>();
                        int totalUsage = 0;
                        foreach (int value in blockDict.Values)
                        {
                            totalUsage += value;
                        }
                        foreach (BlockCondition blockCondition in block.Conditions.Values)
                        {
                            if (blockCondition.TimeCriteria.Contains(DateTime.Now.ToString("dddd")))
                            {
                                if (totalUsage > Convert.ToInt32(blockCondition.Criteria[0]))
                                {
                                    TerminateProgramByPID(pid);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public static void TerminateProgramByPID(int pid)
    {
        if (System.Diagnostics.Process.GetProcesses().Any(x => x.Id == pid))
        {
            System.Diagnostics.Process.GetProcessById(pid).Kill();
        }
    }

    public static Dictionary<string, object> GetBlockStatus()
    {
        var blockStatusDict = JsonConvert.DeserializeObject<Dictionary<string,object>>(File.ReadAllText("user\\blockstatus.json"));
        return blockStatusDict;
    }

    public static void WriteBlockStatus(Dictionary<string, object> input)
    {
        using (StreamWriter writer = new StreamWriter("user\\blockstatus.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(input));
        } 
    }
}

public class Block
{
    public string Name { get; set; }
    public bool Status { get; set; }
    public string Type { get; set; }
    public List<string> Programs { get; set; }
    public Dictionary<int,BlockCondition> Conditions { get; set; }
}

public class BlockCondition
{
    public List<string> Criteria;
    public List<string> TimeCriteria;
}