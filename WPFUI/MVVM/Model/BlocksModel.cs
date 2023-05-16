using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WPFUI.MVVM.Model;

public class BlocksModel
{
    public static Dictionary<string,Block> GetAllBlocks()
    {
        var blockList = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        return (blockList);
    }

    public static void CreateNewBlock(string name, string type, List<string> programs, List<BlockCondition> conditions)
    {
        var existingBlocks = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        existingBlocks[name] = new Block { Status = true, Type = type, Programs = programs, Conditions = conditions};
        using (StreamWriter writer = new StreamWriter("user\\blocks.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(existingBlocks));
        }
        /*CHECK IF SELECTED PROGRAM IS IN DATABASE AND IF NOT THEN ADD TO DATABASE*/
    }

    public void CheckAllBlocks()
    {
        
    }
}

public class Block
{
    public bool Status;
    public string Type;
    public List<string> Programs;
    public List<BlockCondition> Conditions;
}

public class BlockCondition
{
    public List<string> Criteria;
    public List<string> Days;
}