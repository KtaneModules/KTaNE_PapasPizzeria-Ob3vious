using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class papasPizzeriaScript : MonoBehaviour
{
    //public stuff
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public List<MeshRenderer> Comp;
    public GameObject Pizza;
    public GameObject Box;
    public List<TextMesh> Texts;
    public KMBombModule Module;

    //private stuff
    private int day;
    private int customer;
    private string request = String.Empty;
    private int selected = 0;
    private int done = 0;
    private List<int> solution;
    private List<int> submission = new List<int> { };
    private bool open = false;
    private bool isAnimating = false;
    private bool solved = false;

    private List<GameObject> pieces = new List<GameObject> { };

    //logging
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        for (int i = 0; i < 3; i++)
        {
            int x = i;
            Buttons[i].OnInteract += delegate
            {
                if (!solved && open)
                {
                    Audio.PlaySoundAtTransform("Click", Box.transform);
                    Buttons[x].AddInteractionPunch(1f);
                    switch (x)
                    {
                        case 0:
                            selected = (selected + 1) % 9;
                            break;
                        case 1:
                            done++;
                            submission.Add(selected);
                            if (done != 11)
                            {
                                pieces.Add(Instantiate(Pizza, Module.transform));
                                Pizza.transform.localEulerAngles = new Vector3(0, done * (360f / 11f), 0);
                            }
                            else
                            {
                                CheckSolve();
                            }
                            break;
                        case 2:
                            selected = (selected + 8) % 9;
                            break;
                    }
                    UpdateSelector();
                }
                return false;
            };
        }
        UpdateSelector();
        Module.GetComponent<KMSelectable>().OnInteract += delegate
        {
            if (!solved && !open)
            {
                StartCoroutine(BoxAnim(true));
            }
            return true;
        };
    }

    void Start()
    {
        day = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }.IndexOf(DateTime.Today.DayOfWeek.ToString());
        Debug.LogFormat("[Papa's Pizzeria #{0}] Bomb started on {1}.", _moduleID, DateTime.Today.DayOfWeek.ToString());
        GenerateSolution();
    }

    private void SetPizza(int idx)
    {
        List<List<int>> Components = new List<List<int>> { new List<int> { 8 }, new List<int> { }, new List<int> { 0, 3 }, new List<int> { 4, 10 }, new List<int> { 2 }, new List<int> { 5, 6 }, new List<int> { 1, 7 }, new List<int> { 0, 2, 5, 8, 9 }, new List<int> { 0, 5, 8 } };
        for (int i = 0; i < Comp.Count(); i++)
            Comp[i].enabled = Components[idx].Contains(i);
    }

    private void UpdateSelector()
    {
        List<string> CompNames = new List<string> { "PP", "QC", "CB", "SS", "CR", "HM", "VT", "SP", "MM" };
        Texts[2].text = CompNames[selected];
        SetPizza(selected);
    }

    private void GenerateSolution()
    {
        bool boss = Rnd.Range(0, 2) == 1;
        Texts[1].color = new Color(boss ? 0.75f : 0, 0, 0);
        request = String.Empty;
        for (int i = 0; i < 3; i++)
            request += Rnd.Range(0, 8);
        customer = Rnd.Range(0, 8);
        request += "ACQBJMSD"[customer];
        Texts[1].text = request;
        List<List<int>> MustHave = new List<List<int>> { new List<int> { 0, 4, 1, 3, 5, 6, 8 }, new List<int> { 7, 5, 6, 0, 3, 1, 2 }, new List<int> { 8, 3, 1, 5, 0, 2, 6 }, new List<int> { 1, 2, 3, 8, 1, 5, 0 }, new List<int> { 5, 0, 1, 3, 4, 6, 8 }, new List<int> { 1, 2, 6, 4, 5, 0, 3 }, new List<int> { 8, 0, 2, 5, 3, 4, 7 }, new List<int> { 4, 5, 3, 2, 0, 6, 6 } };
        List<List<int>> Priorities = new List<List<int>> { new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, new List<int> { 8, 5, 4, 1, 3, 0, 2, 7, 6 }, new List<int> { 1, 6, 0, 7, 3, 8, 2, 5, 4 }, new List<int> { 7, 2, 4, 0, 5, 1, 3, 8, 6 }, new List<int> { 3, 4, 6, 5, 0, 2, 8, 1, 7 }, new List<int> { 7, 8, 0, 1, 2, 3, 5, 6, 4 } };
        List<int> Taken = new List<int> { MustHave[customer][day] };
        int row = (request[0] - '0' >= 6 ? (request[0] - '0') % 5 : (request[0] - '0'));
        Taken.Add(Priorities[row][(Priorities[row].IndexOf(Taken.First()) + request[1] - '0' + 1) % 9]);
        int k = (Priorities[row].IndexOf(Taken.First()) + request[2] - '0' + 1) % 9;
        for (int i = 0; i < 9 && k > -1; i++)
            if (!Taken.Contains(Priorities[row][(i + k) % 9]))
            {
                Taken.Add(Priorities[row][(i + k) % 9]);
                k = -1;
            }
        k = (Priorities[row].IndexOf(Taken.First()) + request[1] + request[2] - (2 * '0') + 2) % 9;
        for (int i = 0; i < 9 && k > -1; i++)
            if (!Taken.Contains(Priorities[row][(i + k) % 9]))
            {
                Taken.Add(Priorities[row][(i + k) % 9]);
                k = -1;
            }
        string binary = (boss ? "1" : "0") + request.Substring(0, 4).Replace("0", "000").Replace("1", "001").Replace("2", "010").Replace("3", "011").Replace("4", "100").Replace("5", "101").Replace("6", "110").Replace("7", "111") + "0";
        List<string> CompNames = new List<string> { "Pepperoni", "Quad Cheese", "Carbonara", "Sea Shore", "Chicken Ranch", "Ham & Mushrooms", "Vegetarian", "Super Papa", "Meaty Mash" };
        List<string> ClientNames = new List<string> { "Angelica", "Carlton", "Quentin", "Brigitte", "Julien", "Michael", "Sullivan", "Damian" };
        Debug.LogFormat("[Papa's Pizzeria #{0}] Order {1}{2}: Serving {3}. The prioritised flavours in order are: {4}.", _moduleID, request.Substring(0, 3), boss ? " (boss)" : "", ClientNames[customer], Taken.Select(x => CompNames[x]).Join(", "));
        List<List<int>> PizzaGraph = new List<List<int>> { new List<int> { 1 }, new List<int> { 3, 2 }, new List<int> { 4, 1, 4 }, new List<int> { 2, 2, 3, 3 }, new List<int> { 1, 3, 1, 2, 1 }, new List<int> { 4, 1, 3, 2, 1, 4 }, new List<int> { 3, 2, 4, 1, 4, 3, 2 }, new List<int> { 1, 3, 1, 2, 3, 1, 2, 1 }, new List<int> { 3, 1, 4, 3, 1, 2, 4, 1, 2 }, new List<int> { 2, 4, 2, 3, 3, 2, 2, 3, 4, 3 }, new List<int> { 4, 1, 3, 1, 4, 1, 4, 1, 2, 1, 4 } };
        solution = new List<int> { };
        k = 0;
        for (int i = 0; i < 11; i++)
        {
            solution.Add(Taken[PizzaGraph[i][k] - 1]);
            k += binary[i] - '0';
        }
        List<string> CompCodes = new List<string> { "PP", "QC", "CB", "SS", "CR", "HM", "VT", "SP", "MM" };
        Debug.LogFormat("[Papa's Pizzeria #{0}] Expected answer: {1}.", _moduleID, solution.Select(x => CompCodes[x]).Join(", "));
    }

    private void CheckSolve()
    {
        bool good = true;
        for (int i = 0; i < 11; i++)
            if (solution[i] != submission[i])
                good = false;
        List<string> CompCodes = new List<string> { "PP", "QC", "CB", "SS", "CR", "HM", "VT", "SP", "MM" };
        if (good)
        {
            Debug.LogFormat("[Papa's Pizzeria #{0}] You submitted {1}. Module Solved!", _moduleID, submission.Select(x => CompCodes[x]).Join(", "));
            Module.HandlePass();
            solved = true;
            Texts[3].color = new Color(0, 0.75f, 0);
            Audio.PlaySoundAtTransform("Solve", Module.transform);
        }
        else
        {
            Debug.LogFormat("[Papa's Pizzeria #{0}] You submitted {1}, but I expected {2}. Strike!", _moduleID, submission.Select(x => CompCodes[x]).Join(", "), solution.Select(x => CompCodes[x]).Join(", "));
            Module.HandleStrike();
            submission = new List<int> { };
            done = 0;
            GenerateSolution();
            Audio.PlaySoundAtTransform("Strike", Module.transform);
        }
        StartCoroutine(BoxAnim(false));
    }

    private IEnumerator BoxAnim(bool open2)
    {
        while (isAnimating)
            yield return null;
        isAnimating = true;
        if (!open)
        {
            Texts[0].text = "5:00";
            Audio.PlaySoundAtTransform("Box", Box.transform);
        }
        for (float t = 0; t < 1f; t += Time.deltaTime * 1.5f)
        {
            float a = t;
            if (!open2)
                a = 1f - a;
            Box.transform.localEulerAngles = Vector3.Lerp(new Vector3(0, 0, 0), new Vector3(100, 0, 0), a);
            yield return null;
        }
        Box.transform.localEulerAngles = new Vector3(open2 ? 100 : 0, 0, 0);
        open = open2;
        isAnimating = false;
        if (!open2)
        {
            foreach (var piece in pieces)
                piece.transform.localScale *= 0;
            pieces.RemoveAll(x => true);
            Pizza.transform.localEulerAngles = new Vector3(0, 0, 0);
        }
        StartCoroutine(Timer());
    }

    private IEnumerator Timer()
    {
        for (int m = 4; m >= 0 && open; m--)
            for (int s = 59; s >= 0 && open; s--)
            {
                Texts[0].text = m + ":" + s.ToString("00");
                yield return new WaitForSeconds(1f);
            }
        if (open)
        {
            Debug.LogFormat("[Papa's Pizzeria #{0}] You were too late. Strike!", _moduleID);
            Module.HandleStrike();
            submission = new List<int> { };
            done = 0;
            GenerateSolution();
            StartCoroutine(BoxAnim(false));
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "'!{0} start' to activate the module. '!{0} submit PP CB VT SS PP' to enter those slices.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToLowerInvariant();
        if (command == "start")
        {
            Module.GetComponent<KMSelectable>().OnInteract();
            yield return "strike";
            yield return "solve";
        }
        else
        {
            if (open)
            {
                if (Regex.IsMatch(command, @"^submit(\s(pp|qc|cb|ss|cr|hm|vt|sp|mm))+$"))
                {
                    MatchCollection matches = Regex.Matches(command.Replace("press", ""), @"(pp|qc|cb|ss|cr|hm|vt|sp|mm)");
                    foreach (Match match in matches)
                        foreach (Capture capture in match.Captures)
                        {
                            List<string> CompCodes = new List<string> { "PP", "QC", "CB", "SS", "CR", "HM", "VT", "SP", "MM" };
                            while (Texts[2].text.ToLowerInvariant() != capture.ToString())
                            {
                                Buttons[(selected - CompCodes.IndexOf(capture.ToString().ToUpperInvariant()) + 9) % 9 >= 5 ? 0 : 2].OnInteract();
                                yield return null;
                            }
                            Buttons[1].OnInteract();
                            yield return null;
                        }
                    yield return "strike";
                    yield return "solve";
                }
                else
                    yield return "sendtochaterror Invalid command.";
            }
            else
            {
                yield return "sendtochaterror Please activate the module first.";
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return true;
        if (open ^ !isAnimating)
        {
            Module.GetComponent<KMSelectable>().OnInteract();
            while (open ^ !isAnimating)
                yield return null;
        }
        while (done < 11)
        {
            while (selected != solution[done])
            {
                Buttons[(selected - solution[done] + 9) % 9 >= 5 ? 0 : 2].OnInteract();
                yield return null;
            }
            Buttons[1].OnInteract();
            yield return null;
        }
    }
}
