using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using static HexCell;

[Serializable]
public class HexCell : MonoBehaviour
{
	[SerializeField] bool isBigCell;
	//more specifically, if this hex has subhexes

	[SerializeField] HexCell[] Neighbors = new HexCell[6];
	[SerializeField] HexCell CenterSubHex;

	Renderer rend;

    public CellPacket information;

	protected float size;
	protected Vector3 Coordinates;

	Texture2D BiomeTexture;

	/// <summary>
	/// Gets point i of a hexagon in local space
	/// </summary>
	/// <param name="center">The location of the center</param>
	/// <param name="i">Index of the point (0-5. further will just loop round)</param>
	/// <returns></returns>
	Vector3 HexCorner(Vector3 center, int i)
	{
		float angle_deg = 60 * i;

		if(!isBigCell)
		{
			angle_deg -= 30; //makes it pointy top (by just stepping back by the difference)
		}

		float angle_rad = Mathf.PI / 180 * angle_deg;
		return new Vector2(center.x + size * Mathf.Cos(angle_rad), center.y + size * Mathf.Sin(angle_rad));
	}

	/// <summary>
	/// Effectivley the Main function of the Hexcell
	/// </summary>
	/// <param name="importCoords">the coordinates of the hexagon</param>
	/// <param name="isBigCellImport">if it should be listed as a big cell or not</param>
	public void CompileSelf(Vector3 importCoords, bool isBigCellImport)
	{
        rend = gameObject.AddComponent<MeshRenderer>();

        information = WorldGen.CompileCellData(this);
		isBigCell = isBigCellImport;

		Coordinates = importCoords;

		if (isBigCell)
		{
			size = WorldGen.hexsize;
			gameObject.layer = 6;
			gameObject.transform.SetParent(GameObject.FindGameObjectsWithTag("BigCell Container")[0].transform);
			//this is a terrible solution but it's all i can think of right now

            SceneVisibilityManager.instance.DisablePicking(gameObject, true);
			//sets the object as unpickable in the scene editor

			WorldGen.BigCellList.Add(this);


		}
		else
		{
			size = WorldGen.hexsize * Mathf.Sqrt(3) / 12f;
			// be EXTREMELY careful changing this value	   ^
			//weird trig because the rotation difference stoping the width & height matching
			gameObject.layer = 7;
			gameObject.transform.SetParent(GameObject.FindGameObjectsWithTag("SmallCell Container")[0].transform);


            WorldGen.SubCellList.Add(this);
        }

		BiomeTexture = Resources.Load<Texture2D>("WorldTextures/" + information.biomeName.ToString());

        DrawSelf(information.height);
		AddMaterial();
	}

	public void DrawSelf(float height)
	{
		/*
		 * pipeline:
		 *		1) setup local vars (13 for the center, top hex and bottom hex
		 *		2) slap on the needed components & make them locals
		 *		3) clear shit for no reason
		 *		4) set UV and Vert 1 to be the center
		 *		5) make the rest points from HexCorner
		 *		6) hardcode the triangle vectors
		 *		7) config the collider, material and renderer
		 *		8) add text child
		 */
		//transform.position = transform.position - new Vector3(0, height, 0);
		Vector3[] Vertices = new Vector3[13];
		Vector2[] UVs = new Vector2[13];

		Mesh mesh = gameObject.AddComponent<MeshFilter>().mesh;
		BoxCollider collider = gameObject.AddComponent<BoxCollider>();

		mesh.Clear();

		//height = WorldGen.rand.Next(1, 5);

		Vertices[0] = new Vector3(0, 0, height);
		UVs[0] = new Vector2(0, 0);

		for (int i = 1; i < 7; i++)
		{
			Vertices[i + 6] = HexCorner(Vertices[0], i - 1);
			Vertices[i] = Vertices[i + 6] + new Vector3(0, 0, height);

			//Debug.Log($"{Vertices[i - 6]}(old) + {new Vector3(0, 0, height)}(mod) = {Vertices[i]}");

			UVs[i] = Vertices[i];
			UVs[i + 6] = Vertices[i];
		}

		mesh.vertices = Vertices;
		mesh.uv = UVs;

		//Debug.Log($"1: {Vertices[1]}\n 7: {Vertices[7]}");

		mesh.triangles = new int[] { 0, 1, 2,            0, 2, 3,            0, 3, 4,
									 0, 4, 5,            0, 5, 6,            0, 6, 1,

									 1, 12, 7,           1, 7, 2,
									 2, 7, 8,            2, 8, 3,
									 3, 8, 9,            3, 9, 4,

									 4, 9, 10,           4, 10, 5,
									 5, 10, 11,          5, 11, 6,
									 6, 11, 12,          6, 12, 1
		};

		//mesh.RecalculateNormals();
		//basically turns on smoothing mode

		collider.size = new Vector3(size*.2f, size * .2f, 0);

		#region debug child
		GameObject Child = new GameObject("TextChild");
		SceneVisibilityManager.instance.DisablePicking(Child, true);
		Child.transform.parent = transform;
		Child.transform.localPosition = new Vector3(0, 0, height);
		Child.transform.Rotate(-180.0f, 0.0f, 0.0f, Space.Self);

		float reductionScale = size / WorldGen.hexsize;

		Child.transform.localScale = new Vector3(reductionScale, reductionScale, reductionScale);

		TextMesh t = Child.AddComponent<TextMesh>();
		t.anchor = TextAnchor.MiddleCenter;
		t.alignment = TextAlignment.Center;
		t.fontSize = 12;
		t.text = Coordinates.ToString();
		//t.text = $"{Coordinates.x}\n\n\n{Coordinates.y}        {Coordinates.z}";
        t.text = $"\n({Coordinates.x}, {Coordinates.y}, {Coordinates.z})";
		#endregion
        transform.Rotate(-90.0f, 0.0f, 0.0f, Space.Self);
		gameObject.isStatic = true;
	}

	void AddMaterial()
	{
        rend.material = new Material(Shader.Find("Standard"));

        if (isBigCell)
		{
			//Debug.Log("is big cell, ignoring");
			rend.material.SetOverrideTag("RenderType", "Transparent");
			rend.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			rend.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			rend.material.SetInt("_ZWrite", 0);
			rend.material.DisableKeyword("_ALPHATEST_ON");
			rend.material.EnableKeyword("_ALPHABLEND_ON");
			rend.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			//this is taken from StandardShaderGUI.cs, in the unity source code.
			//you can't modify the render type, but you can apply the changes that unity would have if you could

			rend.material.color = new Color(0, 0, 0, 0);
		}
		else
		{
			//Debug.Log("WorldTextures/" + biomeName);

			rend.material.mainTexture = BiomeTexture;
			rend.material.mainTextureScale = new Vector2(.7f, .7f);
			rend.material.mainTextureOffset = new Vector2(.5f, .7f);


        }
		//rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		//rend.receiveShadows = false;
	}

    public enum DisplayType
    {
        Normal, Precipitation, Temperature
    }

    public void UpdateColor(DisplayType dt)
    {
		switch (dt)
		{
			case DisplayType.Normal:
                rend.material.mainTexture = BiomeTexture;
                rend.material.color = Color.white; break;
			case DisplayType.Precipitation:
				rend.material.mainTexture = null;
                rend.material.color = Color.Lerp(WorldGen.PrecipiationColourLow, WorldGen.PrecipiationColourHigh, information.pricipitationNormalised); break;
            case DisplayType.Temperature:
				rend.material.mainTexture = null;
                rend.material.color = Color.Lerp(WorldGen.TemperatureColourLow, WorldGen.TemperatureColourHigh, information.temperatureNormalised); break;
			default:
				break;
        }
    }



    public List<List<HexCell>> CreateSubHexes(Vector3 CenterCoords)
	{
		GameObject obj = new GameObject("subOrigin");
		obj.transform.position = transform.position;
		CenterSubHex = obj.AddComponent<HexCell>();
		CenterSubHex.CompileSelf(CenterCoords, false);
		//Debug.Log("created origin");
		List<List<HexCell>> list = new List<List<HexCell>>
		{
			CenterSubHex.CreateNeighbors()
		};

		//Debug.Log("created 1st set of neighbors");

		for (int i = 0; i < 3; i++)
		{
			//editing a list while it's being used is ILLEGAL, so .ToList() dupes it
			foreach (List<HexCell> subList in list.ToList())
			{
				foreach (HexCell cell in subList)
				{
					list.Add(cell.CreateNeighbors());
				}
			}
		}
		WorldGen.subCellCounter = 0;
		return list;
	}

	/// <summary>
	/// this is used liberally, but it should be fine as it only checks blank spaces rather than completly rewrite
	/// </summary>
	/// <param name="TrF">transform of the cell checking for neighbors</param>
	void CheckForNeighbors()
	{
		//Debug.Log("Checking as " + transform.gameObject.name);
		for (int i = 0; i < Neighbors.Length; i++)
		{
			if (!Neighbors[i])
			{
				Vector3 checkPos = transform.position + WorldGen.GetTrueDirection(i, size);
				//Debug.Log("	checking " + newPos + "\n" + TrF.position + "\n" + WorldGen.TrueDirections[i]);

				Collider[] hitColliders = Physics.OverlapSphere(checkPos, size, 1<<gameObject.layer);
				if (hitColliders.Length == 0)
				{
					continue;
				}
				else if (hitColliders[0].gameObject.TryGetComponent(out HexCell hex))
				{
					Neighbors[i] = hex;
				}
			}
		}
	}

	public List<HexCell> CreateNeighbors()
	{
		//Debug.Log(name + " is creating neighbours");
		List<HexCell> list = new List<HexCell>();

		CheckForNeighbors();
		for (int i = 0; i < Neighbors.Length; i++)
		{
			if (!Neighbors[i])
			{
				Vector3 newCoords = Coordinates + WorldGen.CubicDirections[i];
				Vector3 newPos = transform.position + WorldGen.GetTrueDirection(i, size);

				//Debug.Log($" i  : {i}\npos: {transform.position}\n mod: {TrueDirections[i]}\nnew: {newPos}");
				GameObject obj = new GameObject(isBigCell ? "BigCell " + i : "SubCell " + WorldGen.subCellCounter);
				WorldGen.subCellCounter++;
				obj.transform.position = newPos;

				HexCell Hex = obj.AddComponent<HexCell>();

				Hex.CompileSelf(newCoords, isBigCell);

				Neighbors[i] = Hex;
				list.Add(Hex);

				if (isBigCell)
				{
					Hex.CreateSubHexes(WorldGen.CubicDirections_Diagonal[i]);
				}
			}
		}

		for (int i = 0; i < Neighbors.Length; i++)
		{
			Neighbors[i].CheckForNeighbors();
		}
		return list;
	}

	public void GetCenterSubHex()
	{
		Collider[] hitColliders = Physics.OverlapSphere(transform.position, size * .2f, 1>>7);

		if (hitColliders.Length > 1)
		{
			Debug.LogWarning("Multiple colliders found in LayerMask. Proceeding with first in list.");
		}
		if (!hitColliders[0].gameObject.TryGetComponent(out HexCell target))
		{
			Debug.LogError("First collider does not have HexCell component.");
			return;
		}
		CenterSubHex = target;
	}

}

/// <summary>
/// This just adds buttons to the inspector
/// </summary>
[CustomEditor(typeof(HexCell))]
public class ObjectBuilderEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		HexCell hex = (HexCell)target;
		if (GUILayout.Button("Create Neighbors"))
		{
			hex.CreateNeighbors();
		}
		if (GUILayout.Button("Create SubHexes"))
		{
			hex.CreateSubHexes(new Vector3(0, 0, 0));
		}

		//##################################

        if (GUILayout.Button("Display Normal"))
        {

            foreach (HexCell cell in WorldGen.SubCellList)
            {
                cell.UpdateColor(HexCell.DisplayType.Normal);
            }
        }

        if (GUILayout.Button("Display Precipitation"))
        {

            foreach (HexCell cell in WorldGen.SubCellList)
            {
                cell.UpdateColor(HexCell.DisplayType.Precipitation);
            }
        }

        if (GUILayout.Button("Display Temperature"))
        {
            foreach (HexCell cell in WorldGen.SubCellList)
            {
                cell.UpdateColor(HexCell.DisplayType.Temperature);
            }
        }
    }
}