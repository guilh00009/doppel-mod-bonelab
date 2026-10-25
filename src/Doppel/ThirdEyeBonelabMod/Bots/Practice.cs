using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace ThirdEyeBonelabMod.Bots;

public class Practice
{
	private readonly string label;

	private readonly string fileName;

	private readonly string[] names;

	private readonly float[] first;

	private readonly float[] least;

	private readonly float[] most;

	private readonly float better;

	private readonly float muchWorse;

	private float[] best;

	private float bestScore = -1f;

	private int runs;

	private int kept;

	private float[] trial;

	private IntPtr trialBy;

	private float trialSum;

	private int trialRuns;

	private int sinceBase;

	private string trialWhat = "";

	private string File => Path.Combine(AssetFiles.SaveFolder(""), fileName);

	public Practice(string label, string fileName, string[] names, float[] first, float[] least, float[] most, float better, float muchWorse)
	{
		this.label = label;
		this.fileName = fileName;
		this.names = names;
		this.first = first;
		this.least = least;
		this.most = most;
		this.better = better;
		this.muchWorse = muchWorse;
	}

	private void Load()
	{
		if (best != null)
		{
			return;
		}
		best = (float[])first.Clone();
		try
		{
			if (!System.IO.File.Exists(File))
			{
				return;
			}
			string[] array = System.IO.File.ReadAllLines(File);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split('=');
				if (array2.Length == 2 && float.TryParse(array2[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				{
					int num = Array.IndexOf(names, array2[0]);
					if (num >= 0)
					{
						best[num] = Mathf.Clamp(result, least[num], most[num]);
					}
					else if (array2[0] == "score")
					{
						bestScore = result;
					}
					else if (array2[0] == "runs")
					{
						runs = (int)result;
					}
					else if (array2[0] == "kept")
					{
						kept = (int)result;
					}
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private void Save()
	{
		try
		{
			List<string> list = new List<string>();
			for (int i = 0; i < names.Length; i++)
			{
				list.Add(names[i] + "=" + best[i].ToString("0.###", CultureInfo.InvariantCulture));
			}
			list.Add("score=" + bestScore.ToString("0.###", CultureInfo.InvariantCulture));
			list.Add("runs=" + runs);
			list.Add("kept=" + kept);
			System.IO.File.WriteAllLines(File, list);
		}
		catch (Exception)
		{
		}
	}

	public void Reset()
	{
		best = (float[])first.Clone();
		bestScore = -1f;
		runs = (kept = 0);
		trial = null;
		Save();
	}

	public string Said()
	{
		Load();
		List<string> list = new List<string>();
		for (int i = 0; i < names.Length; i++)
		{
			list.Add(names[i] + " " + best[i].ToString("0.00") + ((Mathf.Abs(best[i] - first[i]) > 0.005f) ? (" (was " + first[i].ToString("0.00") + ")") : ""));
		}
		return runs + " goes scored, " + kept + " changes kept, usual score " + ((bestScore < 0f) ? "none yet" : bestScore.ToString("0.00")) + ": " + string.Join(", ", list) + ((trial != null) ? ("; trying " + trialWhat + " (" + trialRuns + " of 2)") : "");
	}

	public float[] Begin(IntPtr who, out bool trying)
	{
		Load();
		trying = false;
		if (bestScore < 0f || sinceBase >= 2 || (trial != null && trialBy != who))
		{
			return (float[])best.Clone();
		}
		if (trial == null)
		{
			trial = (float[])best.Clone();
			List<string> list = new List<string>();
			int num = ((UnityEngine.Random.value < 0.5f) ? 1 : 2);
			for (int i = 0; i < num; i++)
			{
				int num2 = UnityEngine.Random.Range(0, names.Length);
				float num3 = UnityEngine.Random.Range(0.08f, 0.22f) * ((UnityEngine.Random.value < 0.5f) ? (-1f) : 1f);
				float num4 = trial[num2];
				trial[num2] = Mathf.Clamp(trial[num2] * (1f + num3), least[num2], most[num2]);
				if (Mathf.Abs(trial[num2] - num4) > 0.001f)
				{
					list.Add(names[num2] + " " + num4.ToString("0.00") + " -> " + trial[num2].ToString("0.00"));
				}
			}
			if (list.Count == 0)
			{
				trial = null;
				return (float[])best.Clone();
			}
			trialWhat = string.Join(", ", list);
			trialBy = who;
			trialSum = 0f;
			trialRuns = 0;
		}
		trying = true;
		return (float[])trial.Clone();
	}

	public void End(IntPtr who, bool trying, float score, string how)
	{
		Load();
		runs++;
		if (!trying || trial == null || trialBy != who)
		{
			sinceBase = 0;
			bestScore = ((bestScore < 0f) ? score : Mathf.Lerp(bestScore, score, 0.3f));
			Lab.Event(label + ": as it is now, " + how + " (its usual " + bestScore.ToString("0.00") + ")");
			Save();
			return;
		}
		sinceBase++;
		trialSum += score;
		trialRuns++;
		if (trialRuns == 1 && score > bestScore - muchWorse)
		{
			Lab.Event(label + ": trying " + trialWhat + ", " + how + " (its usual " + bestScore.ToString("0.00") + "), once more");
			Save();
			return;
		}
		float num = trialSum / (float)trialRuns;
		if (trialRuns >= 2 && num > bestScore + better)
		{
			best = trial;
			bestScore = Mathf.Lerp(bestScore, num, 0.5f);
			kept++;
			Lab.Event(label + ": KEPT " + trialWhat + " (" + num.ToString("0.00") + " over two goes; now its usual " + bestScore.ToString("0.00") + ")");
		}
		else
		{
			Lab.Event(label + ": " + trialWhat + " was no better (" + how + "; mean " + num.ToString("0.00") + " against " + bestScore.ToString("0.00") + "), dropped");
		}
		trial = null;
		Save();
	}
}
