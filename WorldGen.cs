using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UI;
using static WorldGen;
using static HexCell;
using Unity.VisualScripting.FullSerializer;


//https://www.redblobgames.com/grids/hexagons/
public class WorldGen
{
	#region "global" variables
	public static int subCellCounter = 0;

    //how a hexagon's coordinates need to be altered to reach it's neighbour.
    //for example, to reach the down-right cell, you have to alter it by (+1, -1, 0).

	//the comments descibe this transformation in actual space
	//									//flat-top hexagons	(big hexes)		//point-top hexagons (subhexes)
    public static readonly Vector3[] CubicDirections = {
		new Vector3(+1, -1, 0),			//x+=width*.75, y-=height*.5		//x+=width
		new Vector3(+1, 0, -1),			//x+=width*.75, y+=height*.5		//x+=width*.5, y+=height*.75
		new Vector3(0, +1, -1),			//y+=height							//x-=width*.5, y+=height*.75
		new Vector3(-1, +1, 0),			//x-=width*.75, y+=height*.5		//x-=width
		new Vector3(-1, 0, +1),			//x-=width*.75, y-=height*.5		//x-=width*.5, y-=height*.75
		new Vector3(0, -1, +1)			//y-=height							//x+=width*.5, y-=height*.75
	};

	//i havev genuinly no idea what these do and alterning them seems to make no difference
    public static readonly Vector3[] CubicDirections_Diagonal = {
		new Vector3(+4, -8, +4),		//x+=width*6, y-=height*3
		new Vector3(+8, -4, -4),		//x+=width*6, y+=height*3
		new Vector3(+4, +4, -8),		//x+=0,		  y+=height*6
		new Vector3(-4, +8, -4),		//x-=width*6, y+=height*3
		new Vector3(-8, +4, +4),		//x-=width*6, y-=height*3
		new Vector3(-4, -4, +8)			//x+=0,		  y-=height*6
	};

    //the radius of the bigcells. doesn't change the quantity of the subhexes (60), but does change the size
    public static float hexsize = 5f;

	//holds the hight, precipitation and temperature noise maps
	public static RandomHolder randomHolder = new RandomHolder();

	public static Color PrecipiationColourLow =	 new Color(0f, 0f, 0f, 0f);
    public static Color PrecipiationColourHigh = new Color(0f, 1f, 0.9f, 0f);
    public static Color TemperatureColourLow =   new Color(0f, 1f, 0.9f, 0f);
    public static Color TemperatureColourHigh =	 new Color(1f, 0f, 0f, 0f);

	public static List<HexCell> BigCellList;
	public static List<HexCell> SubCellList;

	public static GameObject Knob; //for the graphs
	public static GameObject Chart; //for the graphs

    #endregion


    public static HexCell Main()
	{
        BigCellList = new List<HexCell>();
        SubCellList = new List<HexCell>();

        //wangs a hexagon in the middle of the world
        GameObject originObj = new GameObject("Origin");
		originObj.transform.position = new Vector3(0, 0, 0);
		HexCell originHex = originObj.AddComponent<HexCell>();
		originHex.CompileSelf(new Vector3(0, 0, 0), true);
		//for some reason hexcell.hexcell doesnt work?
		//i mean it doesnt make much a difference to me, it just looks messy

		//puts subhexes in that hexagon
		originHex.CreateSubHexes(new Vector3(0, 0, 0));
		return originHex;
	}

	public static Vector3 GetTrueDirection(int index, float size)
	{
		bool flat = size == hexsize;
		float width;
		float height;

		if (flat)
		{
			width = size * 2;
			height = (float)(size * Math.Sqrt(3));
		}
		else
		{
			width = (float)(size * Math.Sqrt(3));
			height = size * 2;
		}

		switch (index)
		{
			case 0:
				return flat ? new Vector3(width * .75f, 0, height * -.5f) : new Vector3(width, 0, 0);
			case 1:
				return flat ? new Vector3(width * .75f, 0, height * .5f) : new Vector3(width * .5f, 0, height * .75f);
			case 2:
				return flat ? new Vector3(0, 0, height) : new Vector3(width * -.5f, 0, height * .75f);
			case 3:
				return flat ? new Vector3(width * -.75f, 0, height * .5f) : new Vector3(-width, 0, 0);
			case 4:
				return flat ? new Vector3(width * -.75f, 0, height * -.5f) : new Vector3(width * -.5f, 0, height * -.75f);
			case 5:
				return flat ? new Vector3(0, 0, -height) : new Vector3(width * .5f, 0, height * -.75f);
			default:
				Debug.LogError("out of range exception");
				return new Vector3(0, 0, 0);
		}
	}
	public static Vector3 GetTrueDirection_Diagonal(int index, float size)
	{
		float width = (float)(size * Math.Sqrt(3));
		float height = size * 2;
		
		switch (index)
		{
			case 0:
				return new Vector3(width*6, 0, height*-3);
			case 1:
				return new Vector3(width*6, 0, height * 3);
			case 2:
				return new Vector3(0, 0, height * 6);
			case 3:
				return new Vector3(width * -6, 0, height * 3);
			case 4:
				return new Vector3(width * -6, 0, height * -3);
			case 5:
				return new Vector3(0, 0, height * -6);
			default:
				Debug.LogError("out of range exception");
				return new Vector3(0, 0, 0);
		}
	}

	/// <summary>
	/// reads what the temperature, precipitation and height will be at that point, and therefore biome, and puts it in a struct
	/// </summary>
	/// <param name="Recepient">the cell the compiled struct is being delivered to</param>
	/// <returns></returns>
	public static CellPacket CompileCellData(HexCell Recepient) 
	{
		Vector3 location = Recepient.gameObject.transform.position;

		float[] temper = randomHolder.GetValue("Temperature", location.x, location.z);
		float[] precip = randomHolder.GetValue("Precipitation", location.x, location.z);
		float height = randomHolder.GetValue("Height", location.x, location.z)[0];
        Vector2 vector = new Vector2(temper[1], precip[1]);

        //Debug.Log($"temp: {temp}\nprec: {randomHolder.GetValue("Precipitation", location.x, location.z)}\nnew pre: {precip} ");

        //Debug.Log($"height: {height}\ntemp: {temper[1]}°\nprecip: {precip[1]}cm");

        /* Time for some explaining. The way we're going to be deciding biomes is with this graph:
		* https://upload.wikimedia.org/wikipedia/commons/6/68/Climate_influence_on_terrestrial_biome.svg
		* 
		* The problem here is that it's triangular, but the values we're generating should saturate the whole thing.
		* The first option was to just flip the graph over, but if we want to have the precipitation and temperature
		* values shown, this wont work. "mmm, -10°c and heavy snowfall? looks like subtropical desert."
		* We need to properly remap the values.
		* 
		* I asked some friends about it, and their solution was "If we can guarantee that the reflection line is at
		* 45° in the picture, then... yeah. We could just normalise them to the interval [0, 1], flip, then denormalise."
		* 
		* "By my maths, the point P(x, y), reflected over the line y=mx+b should map to P(x', y'), with:
		*
		*	x' = -x + 2(my - x - mb)/(m² + 1)
		*	y' = y - 2x/m - 2(my - x - mb)/(m³ + m) 
		* "
		* 
		* additionally, because i need to write a function to see if it's above a line to know if it needs flipping,
		* i got to turn our brackets from this mess:https://imgur.com/a/fg5pMaZ to this thing of beauty https://imgur.com/a/CcZFGVI
		* it is a significantly slower beauty, but eh
		*/

        #region remapping the points that fell outside the chart back onto it
        //dont need the function for this one, the main line is (y = 8.3 x + 175)
		bool remapped = false;

        if (!IsBelow(new Vector2(-10, 0), new Vector2(32, 420), vector)) {
			remapped = true;

            /*
			 	float m = 8.3f;
				float c = 175;

				x' = -x + 2(my - x - mb)/(m² + 1)
				y' = y - 2x/m - 2(my - x - mb)/(m³ + m) 
				 x = -x + 2*(m * y - x - m * c) / (Mathf.Pow(m, 2) + 1);
				 y = y - 2 * x / m - 2*(m * y - x - m * c) / (Mathf.Pow(m, 3) + m);
				lil came up with this method, but it doesnt behave well with the finite boundaries of the chart
				(and other weirdness but that's not important right now)
				
				i ran this problem through chatGPT and all its solutions didnt fucking work so i had to read up
				on a shit load of high school maths i had long-since forgotton.
			*/

            /*
				the function of the dividing line is y = 10x + 100
				the closest point on the line from another outside point is perpendicular to the main line
				the perpendicular gradient of the line is the opposite and reciprocal of the main line's gradient.
				10 -> 1/10 -> -1/10  (y = -.1x + c)
				it's the offset that actually needs to be calculated per-point, which we do by plugging in the x and y of the point
			*/
            float c = vector.y + 0.1f * vector.x;
            //so now we have the y = mx + c of the line, we just need the point of intersection (which will be the closest point)
			

            float intersec_x = (100 - c) / -10.1f;
            float intersec_y = 10 * intersec_x + 100;

			Vector2 intersection = new Vector2(intersec_x, intersec_y);

			float distanceToDivider = Vector2.Distance(vector, intersection);
            //how far from the dividing line the point is

            /*
				one of the main problems with mirroring is that at the extremities, it will always end up out of bounds. to prevent this,
				instead of mirroring, im going to apply a scale factor towards the bottom right corner relative to the point's distance
				from the line. 

				Vector2 newPosition = Vector2.LerpUnclamped(point, intersection, 2);
				is an effectie way of just mirroring
			 */

            float m1 = (vector.y - 0) / (vector.x - 32);
            float c1 = vector.y - m1 * vector.x;
			//the y=mx+c of a line from the point to the bottom right corner

			float distanceFromCorner = Vector2.Distance(vector, new Vector2(32, 0));

			float percentageDistance = (distanceToDivider / distanceFromCorner)*10;
			
			Vector2 newPosition = Vector2.Lerp(vector, new Vector2(32, 0), percentageDistance);


			//Debug.Log($"{distanceToDivider}, {distanceFromCorner}, {percentageDistance}");

            temper[1] = newPosition.x;
            precip[1] = newPosition.y;

            vector = new Vector2(temper[1], precip[1]);
			//refresh the vector to the biome picker can use it

            //assigning new normalised values
            temper[0] = Mathf.InverseLerp(-10, 32, temper[1]);
			//Debug.Log($"-10, 32. {temper[1]}, {Mathf.InverseLerp(-10, 32, temper[1])}");

            precip[0] = Mathf.InverseLerp(0, 420, precip[1]);


        }
        #endregion

        #region biome checking
        Biome biomeName;

        if (IsBelow(new Vector2(25f, 360), new Vector2(20.5f, 0), vector))
		{
			if (IsBelow(new Vector2(21, 50), new Vector2(32, 100), vector))
			{
				biomeName = Biome.SUBTROPICAL_DESERT;
			}
			else if (IsBelow(new Vector2(23, 230), new Vector2(32, 280), vector))
			{
				biomeName = Biome.SHRUBLAND; // "tropical seasonal forest / savannah" on the chart
            }
			else
			{
				biomeName = Biome.TROPICAL_RAINFOREST;
            }
		}
		else if (IsBelow(new Vector2(8.5f, 185), new Vector2(5.5f, 0), vector))
		{
			if (IsBelow(new Vector2(5.5f, 30), new Vector2(21, 55), vector))
			{
				biomeName = Biome.TEMPERATE_GRASSLAND; // "/cold desert" on the chart
            }
			else if (IsBelow(new Vector2(6, 45f), new Vector2(22, 130), vector))
			{
				biomeName = Biome.WOODLAND; // "/shrubland" on the chart
            }
			else if (IsBelow(new Vector2(8.5f, 175), new Vector2(23.3f, 235), vector))
			{
				biomeName = Biome.TEMPERATE_DECIDUOUS_FOREST; // "temperate seasonal forest" on the chart
            }
			else
			{
				biomeName = Biome.TEMPERATE_RAINFOREST;
            }
		}
		else if (IsBelow(new Vector2(1.5f, 115), new Vector2(-1, 0), vector))
		{
			if (IsBelow(new Vector2(6f, 30), new Vector2(-1, 10), vector))
			{
				biomeName = Biome.TEMPERATE_GRASSLAND; // "/cold desert" on the chart

            }
			else if (IsBelow(new Vector2(-.5f, 25), new Vector2(6, 45), vector))
			{
				biomeName = Biome.WOODLAND; // "/shrubland" on the chart
            }
			else
			{
				biomeName = Biome.TAIGA; // "boreal forest" on the chart
            }
		}
		else
		{
			biomeName = Biome.TUNDRA;
        }
        #endregion

        #region placing knobs

        float x1 = Mathf.Lerp(0, 79.5f, temper[0]);
        float y1 = Mathf.Lerp(0, 79.1f, precip[0]);

        GameObject knob = GameObject.Instantiate(Knob, new Vector3(0, 0, 0), Quaternion.identity, Chart.transform);
        RectTransform rect = knob.GetComponent<RectTransform>();

        Image img = knob.GetComponent<Image>();


        rect.localPosition = new Vector3(x1, y1, 0);

        switch (biomeName)
        {
			//CHECK DOCUMENTS FOR THE PYTHON CONVERTER
            case Biome.SUBTROPICAL_DESERT:
				img.color = new Color(0.78f, 0.45f, 0.2f, 1);
                break;
            case Biome.SHRUBLAND:
                img.color = new Color(0.59f, 0.65f, 0.15f, 1);
                break;
            case Biome.TROPICAL_RAINFOREST:
                img.color = new Color(0.03f, 0.33f, 0.18f, 1);
                break;
            case Biome.TEMPERATE_GRASSLAND:
                img.color = new Color(0.57f, 0.49f, 0.19f, 1);
                break;
            case Biome.WOODLAND:
                img.color = new Color(0.7f, 0.49f, 0.03f, 1);
                break;
            case Biome.TEMPERATE_DECIDUOUS_FOREST:
                img.color = new Color(0.18f, 0.54f, 0.63f, 1);
                break;
            case Biome.TEMPERATE_RAINFOREST:
                img.color = new Color(0.04f, 0.33f, 0.43f, 1);
                break;
            case Biome.TAIGA:
                img.color = new Color(0.36f, 0.56f, 0.32f, 1);
                break;
            case Biome.TUNDRA:
                img.color = new Color(0.58f, 0.65f, 0.68f, 1);
                break;

            default:
				img.color = Color.white;
				break;
        }

        if (remapped)
        {
            img.color = Color.red;
        }

        #endregion
        CellPacket cellPacket = new CellPacket
		{
			height = randomHolder.GetValue("Height", location.x, location.z)[0],
			temperature = temper[1],
            temperatureNormalised = temper[0], // -10 to 32
            pricipitation = precip[1],
            pricipitationNormalised = precip[0], //0 to 420
            biomeName = biomeName,	//cell uses the name to load the material.
        };

        return cellPacket;
	}

	/// <summary>
	/// checks if a point is below a line on a graph
	/// </summary>
	/// <param name="point1">the first point the line intersects</param>
	/// <param name="point2">the second point the line intersects</param>
	/// <param name="check">the point on the graph being checked</param>
	/// <returns>if the point is below the line</returns>
	public static bool IsBelow(Vector2 point1, Vector2 point2, Vector2 check)
	{
		float m = (point1.y - point2.y) / (point1.x - point2.x);
        float c = point1.y - m * point1.x;
		//y=mx+c

		return check.y < m * check.x + c;

	}

    public enum Biome
    {
        SUBTROPICAL_DESERT,
        SHRUBLAND,
        TROPICAL_RAINFOREST,
        TEMPERATE_GRASSLAND,
        WOODLAND,
        TEMPERATE_DECIDUOUS_FOREST,
        TEMPERATE_RAINFOREST,
        TAIGA,
        TUNDRA,
		NONE
    }
}


[System.Serializable]
public class CellPacket{
	public float height;
	public Biome biomeName;
	public float temperature;
	public float temperatureNormalised;
	public float pricipitation;
	public float pricipitationNormalised;
}
/* testing property drawers for custom data displays in the editor
[CustomPropertyDrawer(typeof(CellPacket))]
public class CellPacketDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects
        var biomeNameRect = new Rect(position.x, position.y, 30, position.height);
        var heightRect = new Rect(position.x + 35, position.y, 50, position.height);
        var temperatureRect = new Rect(position.x + 90, position.y, position.width - 90, position.height);

        // Draw fields - pass GUIContent.none to each so they are drawn without labels
        EditorGUI.PropertyField(biomeNameRect, property.FindPropertyRelative("biomeName"), GUIContent.none);
        EditorGUI.PropertyField(heightRect, property.FindPropertyRelative("height"), GUIContent.none);
        EditorGUI.PropertyField(temperatureRect, property.FindPropertyRelative("temperature"), GUIContent.none);

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}
*/
public class RandomHolder {
	public System.Random rand = new System.Random();

    /*
	 * Tried a few different methods with varying success. First was the perlin noise built
	 * into Unity, but you can only have one of them. There aren't any particularly great c#
	 * standalone implimentions. Tried https://github.com/WardBenjamin/SimplexNoise which does
	 * use seeds, but requires you to load the values into a float[,], and to have as many as
	 * we need at the ready it froze unity. Very far from game ready.
	 * 
	 * Ended up with K.jpg's OpenSimplex 2, which is a little hard to use but at least it actually works.
	 * 
	 * the reason we're going through all this effort is 1) simplex is cool and 2) we can have
	 * multiple different maps so they're actually different and biome doesnt directly correlate
	 * with height.
	*/

    readonly float heightFrequency = .03f;
    readonly float temperatureFrequency = .1f;
    readonly float percipitationFrequency = .1f;

    public OpenSimplex2S HeightSimplex;
    public OpenSimplex2S TemperatureSimplex;
    public OpenSimplex2S PrecipitationSimplex;

    public float Heighthighest = -10000;
    public float Heightlowest = 10000;
    public float Temperaturehighest = -10000;
    public float Temperaturelowest = 10000;
    public float Precipitationhighest = -10000;
    public float Precipitationlowest = 10000;

    public RandomHolder()
	{
		Debug.Log("seeding");
        
            long heightSeed = NextLong();
            long temperatureSeed = NextLong();
            long precipitationSeed = NextLong();
            //this is the full range a signed long can hold
            Debug.Log(
                $"heightSeed:        {heightSeed}\n" +
                $"temperatureSeed:   {temperatureSeed}\n" +
                $"precipitationSeed: {precipitationSeed}\n");

            HeightSimplex = new OpenSimplex2S(heightSeed);
            TemperatureSimplex = new OpenSimplex2S(temperatureSeed);
            PrecipitationSimplex = new OpenSimplex2S(precipitationSeed);
    }

    public float[] GetValue(string type, float x, float y)
	{
		float[] result = new float[2];
		//[0] is the nomalised value we get from the map, [1] is the actual value.


		switch (type)
		{
			case "Height":

				//3 octaves of noise
				result[0] = (float)
					(1 * HeightSimplex.Noise2(x * heightFrequency * 1, y * heightFrequency * 1) + 
					0.5 * HeightSimplex.Noise2(x * heightFrequency * 2, y * heightFrequency * 2) +
					0.25 * HeightSimplex.Noise2(x * heightFrequency * 4, y * heightFrequency * 4));
				//   ^ the amplitudes					^ double frequency = double density of the map

				//the way octaves work is multiplying in noisemaps at lower frequencies. the problem is
				//it messes with the data range, but we can divide by the amplitides to squash it down again
				result[0] /= (1 + 0.5f + 0.25f);
				//our ratio of each one being half the previous is called the gain/persistance
				//if you really wanted to, you could make each octave use a different seed

				result[0] = Mathf.Pow(result[0], 1f);
				//a final function to either slightly squish (<1) or stretch things (>1).

				result[0] += 1;
				//bump it up a little bit so nothing goes backwards

				if (result[1] < Heightlowest)
				{
					Heightlowest = result[1];
					//Debug.LogWarning($"Heightlowest: {Heightlowest}");
				}
				else if (result[1] > Heighthighest)
				{
					Heighthighest = result[1];
                    //Debug.LogWarning($"Heighthighest: {Heighthighest}");
                }

                break;
			case "Temperature":
                result[0] = (float)
					(TemperatureSimplex.Noise2(x * temperatureFrequency * 1, y * temperatureFrequency * 1) +
					0.5 * TemperatureSimplex.Noise2(x * temperatureFrequency * 2, y * temperatureFrequency * 2));

                result[0] /= (1 + 0.5f);
				result[0] = Mathf.Pow(result[0], 1f);

                result[0] = (result[0] + 1f) / 2f;
				//shifting the values from [-1, 1] to [0, 2], then halfing it to get a proper 0 - 1 normalisation

				result[0] = (result[0] - 0.1f) * 1.3f;

                result[1] = Mathf.Lerp(-10f, 32f, result[0]);
                //normalise the values to be between -10 and 32, then just lerping)

                //Debug.LogWarning($"lowest: {lowest}\nhighest: {highest}");

                if (result[1] < Temperaturelowest)
                {
                    Temperaturelowest = result[1];
                    //Debug.LogWarning($"Temperaturelowest: {Temperaturelowest}");
                }
                else if (result[1] > Temperaturehighest)
                {
                    Temperaturehighest = result[1];
                    //Debug.LogWarning($"Temperaturehighest: {Temperaturehighest}");
                }

                //0.13

                break;
			case "Precipitation":
                result[0] = (float)
					 (PrecipitationSimplex.Noise2(x * percipitationFrequency * 1, y * percipitationFrequency * 1) +
					 0.5 * PrecipitationSimplex.Noise2(x * percipitationFrequency * 2, y * percipitationFrequency * 2));

                result[0] /= (1 + 0.5f);
                result[0] = Mathf.Pow(result[0], 1f);

                result[0] = (result[0] + 1f) / 2f;

                result[0] = (result[0] - 0.1f) * 1.3f;

                result[1] = Mathf.Lerp(0, 420, result[0]);

                if (result[1] < Precipitationlowest)
                {
                    Precipitationlowest = result[1];
                    //Debug.LogWarning($"Precipitationlowest: {Precipitationlowest}");
                }
                else if (result[1] > Precipitationhighest)
                {
                    Precipitationhighest = result[1];
                    //Debug.LogWarning($"Precipitationhighest: {Precipitationhighest}");
                }

                break;

			default:
				result[0] = result[1] = 1;
				break;
		}

		return result;
        
	}

	//there is no rand.NextLong like you can for ints and doubles,
	//but you can just piece one together.
    private long NextLong()
    {
        byte[] buffer = new byte[8];
        rand.NextBytes(buffer);
        return BitConverter.ToInt64(buffer, 0);
    }
}