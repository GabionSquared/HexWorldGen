using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class Click : MonoBehaviour
{
	//all our gubbins are in WorldGen.cs, this is just an interface for it

	[SerializeField] GameObject knob;
	[SerializeField] GameObject chart;

	[SerializeField] Text LowestPrecip;
	[SerializeField] Text LowestTemp;
	[SerializeField] Text LowestHeight;

	[SerializeField] Text HighestPrecip;
	[SerializeField] Text HighestTemp;
	[SerializeField] Text HighestHeight;

    void Start()
	{
		WorldGen.Knob = knob;
		WorldGen.Chart = chart;


        Button btn = gameObject.GetComponent<Button>();
		btn.onClick.AddListener(TaskOnClick);
	}

    private void Update()
    {
        LowestPrecip.text = WorldGen.randomHolder.Precipitationlowest.ToString();
        LowestTemp.text   = WorldGen.randomHolder.Temperaturelowest.ToString();
        LowestHeight.text = WorldGen.randomHolder.Heightlowest.ToString();

        HighestPrecip.text = WorldGen.randomHolder.Precipitationhighest.ToString();
        HighestTemp.text   = WorldGen.randomHolder.Temperaturehighest.ToString();
        HighestHeight.text = WorldGen.randomHolder.Heighthighest.ToString();
    }

    void TaskOnClick()
	{
		Debug.Log("bruh");

		WorldGen.Main();
	}



	void OnGUI()
	{
		if (GUI.Button(new Rect(10, (0 * 110) + 10, 150, 100), "Create 1"))
		{
			WorldGen.Main();
		}

		if (GUI.Button(new Rect(10, (1 * 110) + 10, 150, 100), "Create 7"))
		{

            WorldGen.Main().CreateNeighbors();
		}

		if (GUI.Button(new Rect(10, (2 * 110) + 10, 150, 100), "Create Many"))
		{
			WorldGen.Main().CreateNeighbors();

            //3 gives roughly 3000 small hexes 61 bigs
            //10 gives roughly 20,000 small hexes
            //30 gives roughly 67,000 small hexes

            for (int i = 0; i < 10; i++)
			{
                //		prevents "Collection was modified; enumeration operation may not execute."
				// by duplicating the existing list
                foreach (HexCell cell in WorldGen.BigCellList.ToList())
				{
					List<HexCell> newlist = cell.CreateNeighbors();
				}
				
			}

			//WorldGen.SubCellList.ElementAt(1).transform.parent.gameObject.SetActive(false);

        }
		
		if (GUI.Button(new Rect(10, (3 * 110) + 10, 150, 100), "Empty"))
		{
			Destroy(GameObject.Find("BigCell Container"));
			Destroy(GameObject.Find("SmallCell Container"));

			GameObject ass = new GameObject("BigCell Container");
			GameObject ass2 = new GameObject("SmallCell Container");

			ass.tag = "BigCell Container";
			ass2.tag = "SmallCell Container";
		}


        if (GUI.Button(new Rect(10, (5 * 110) + 10, 150, 100), "Display Normal"))
        {

            foreach (HexCell cell in WorldGen.SubCellList)
            {
				cell.UpdateColor(HexCell.DisplayType.Normal);
            }
        }

        if (GUI.Button(new Rect(10, (6 * 110) + 10, 150, 100), "Display Precip"))
        {

            foreach (HexCell cell in WorldGen.SubCellList)
            {
                cell.UpdateColor(HexCell.DisplayType.Precipitation);
            }
        }

        if (GUI.Button(new Rect(10, (7 * 110) + 10, 150, 100), "Display Temp"))
        {
            foreach (HexCell cell in WorldGen.SubCellList)
            {
                cell.UpdateColor(HexCell.DisplayType.Temperature);
            }
        }
    }
}