using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WPFUI.MVVM.Model;

public class BlocksModel
{
    public Dictionary<string,Block> GetAllBlocks()
    {
        var allBlocks = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        return (allBlocks);
    }

    public void CreateNewBlock(string name, List<string> programs, List<BlockCondition> conditions)
    {
        var existingBlocks = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        existingBlocks[name] = new Block { Status = true, Programs = programs, Conditions = conditions};
        using (StreamWriter writer = new StreamWriter("programlist.json"))
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
    public List<string> Programs;
    public List<BlockCondition> Conditions;
}

public class BlockCondition
{
    public string Type;
    public List<string> Criteria;
    public List<string> Days;
}