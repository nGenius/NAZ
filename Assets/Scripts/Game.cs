using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    private static readonly int MAX_BOX_COUNT = 6;
    private static readonly float TIME_LIMIT = 60;
    private static readonly int FEVER_INTERVAL = 5;

    private Box[,] boxes = new Box[MAX_BOX_COUNT, MAX_BOX_COUNT];
    private float timeValue = TIME_LIMIT;
    private int comboCount;
    private bool isFeverMode;

    private Box pickBox;
    private List<Box> maxNumberBoxes = new List<Box>();
    private List<Box> feverBoxes = new List<Box>();

    public Text time;
    public Text maxNumber;
    public Text combo;

    private bool gameStart;
    private bool isTouch;
    private bool isPause;
    private bool isFeverEffect;

    private PointerEventData eventDataCurrentPosition
    {
        get
        {
            if (eventData == null)
            {
                eventData = new PointerEventData(EventSystem.current);
            }

            return eventData;
        }
    }
    private PointerEventData eventData;

    private List<RaycastResult> results = new List<RaycastResult>();

    void Awake()
    {
        InitBoxes();
    }

    private void InitBoxes()
    {
        for (int i = 0; i < MAX_BOX_COUNT; i++)
        {
            for (int j = 0; j < MAX_BOX_COUNT; j++)
            {
                if (IsGameBox(i, j) == false)
                {
                    continue;
                }

                boxes[i, j] = (Instantiate(Resources.Load("Box")) as GameObject).GetComponent<Box>();
                boxes[i, j].transform.SetParent(transform, false);
                boxes[i, j].transform.localPosition = new Vector3(
                    (j - (MAX_BOX_COUNT / 2.0f)) * 100.0f + 50,
                    ((MAX_BOX_COUNT / 2.0f) - i) * 100.0f - 50, 0);
                boxes[i, j].SetCell(i, j);
            }
        }
    }

    public void Reset()
    {
        for (int i = 0; i < MAX_BOX_COUNT; i++)
        {
            for (int j = 0; j < MAX_BOX_COUNT; j++)
            {
                if (IsGameBox(i, j) == false)
                {
                    continue;
                }

                boxes[i, j].Reset();
            }
        }
        
        timeValue = TIME_LIMIT;
        comboCount = 0;

        RefreshMaxBox();
    }

    private bool IsGameBox(int i, int j)
    {
        return !((i == 2 || i == 3) && (j == 2 || j == 3));
    }
	
	// Update is called once per frame
	void Update ()
	{
	    if (isPause || isFeverEffect)
	    {
	        return;
	    }

	    if (gameStart == false
	        && Input.GetMouseButtonUp(0) && IsTouchGameStart())
	    {
	        gameStart = true;
	        Reset();
	    }
	    
        if(gameStart == false)
	    {
	        return;
	    }

	    UpdateTime();

	    if (isFeverMode)
	    {
	        UpdateFeverInput();
	    }
	    else
	    {
            UpdateNormalInput();
        }
	}

    private bool IsTouchDown()
    {
#if UNITY_EDITOR
        return Input.GetMouseButtonDown(0);
#elif UNITY_ANDROID || UNITY_IOS
        return isTouch == false && Input.touchCount > 0;
#endif
    }

    private bool IsTouchUp()
    {
#if UNITY_EDITOR
        return Input.GetMouseButtonUp(0);
#elif UNITY_ANDROID || UNITY_IOS
        return isTouch && Input.touchCount == 0;
#endif
    }

    private bool IsTouching()
    {
#if UNITY_EDITOR
        return pickBox != null && Input.GetMouseButton(0);
#elif UNITY_ANDROID || UNITY_IOS
        return pickBox != null && isTouch && Input.touchCount > 0;
#endif
    }

    private void UpdateFeverInput()
    {
        if(IsTouchDown())
        {
            isTouch = true;
            pickBox = GetTouchedBox();

            if (pickBox != null)
            {
                feverBoxes.Add(pickBox);
                pickBox.SetBackgroundColor(Color.gray);
            }
        }
        else if(IsTouchUp())
        {
            isTouch = false;
            if (feverBoxes.Count > 1)
            {
                StartCoroutine(Match(feverBoxes));
            }
            pickBox = null;
        }
        else if(feverBoxes.Count > 0 && IsTouching())
        {
            Box lastBox = feverBoxes[feverBoxes.Count - 1];
            Box box = GetTouchedBox();

            if (lastBox == pickBox && box == pickBox)
            {
                return;
            }

            if (feverBoxes.Count > 1)
            {
                Box targetBox = feverBoxes[feverBoxes.Count - 2];
                if (targetBox == box)
                {
                    feverBoxes.Remove(lastBox);
                    lastBox.SetBackgroundColor(Color.yellow);
                }
            }

            if (feverBoxes.Find(x => x.Equals(box)) == null && IsNearBox(lastBox, box))
            {
                feverBoxes.Add(box);
                box.SetBackgroundColor(Color.gray);
            }
        }
    }

    private void UpdateNormalInput()
    {
        if (IsTouchDown())
        {
            isTouch = true;
            pickBox = GetTouchedBox();
        }
        else if (IsTouchUp())
        {
            isTouch = false;
            if (pickBox != null)
            {
                Box resultBox = GetTouchedBox();

                if (IsNearBox(pickBox, resultBox))
                {
                    Match(resultBox);
                }
            }
            pickBox = null;
        }
    }

    public IEnumerator Match(List<Box> boxes)
    {
        isFeverEffect = true;

        Box resultBox = boxes[boxes.Count - 1];

        for (int i = 0; i < boxes.Count - 1; i++)
        {
            if (resultBox == boxes[i + 1])
            {
                boxes[i + 1].Sum(boxes[i]);
            }
            else
            {
                boxes[i + 1].AbsoluteSum(boxes[i]);
                boxes[i + 1].text.color = Color.cyan;
            }
            
            boxes[i].ClearNumber();

            yield return new WaitForSeconds(0.1f);
        }

        for (int i = 0; i < boxes.Count - 1; i++)
        {
            boxes[i].Reset();
        }

        boxes.Clear();

        RefreshMaxBox();
        RefreshCombo(resultBox);

        isFeverEffect = false;
    }

    private void Match(Box resultBox)
    {
        resultBox.Sum(pickBox);
        pickBox.Reset();
        RefreshMaxBox();
        RefreshCombo(resultBox);
    }

    private bool IsNearBox(Box sourceBox, Box resultBox)
    {
        return resultBox != null && sourceBox != resultBox && sourceBox.IsNearBox(resultBox);
    }

    private void RefreshCombo(Box resultBox)
    {
        if (resultBox.alive)
        {
            comboCount++;
            SetFeverMode(comboCount % FEVER_INTERVAL == 0);
        }
        else
        {
            SetFeverMode(false);
            comboCount = 0;
        }

        if (isFeverMode)
        {
            combo.text = "-FEVER-";
            combo.color = Color.yellow;
        }
        else
        {
            combo.text = comboCount == 0 ? "" : comboCount + " COMBO";
            combo.color = Color.green;
        }
    }

    private void SetFeverMode(bool isFever)
    {
        isFeverMode = isFever;
        ChangeAllBoxColors(isFeverMode ? Color.yellow : Color.white);

        if (isFeverMode)
        {
            feverBoxes.Clear();
        }
    }

    private void ChangeAllBoxColors(Color color)
    {
        for (int i = 0; i < MAX_BOX_COUNT; i++)
        {
            for (int j = 0; j < MAX_BOX_COUNT; j++)
            {
                if (IsGameBox(i, j) == false || boxes[i, j].alive == false)
                {
                    continue;
                }

                boxes[i, j].SetBackgroundColor(color);
            }
        }
    }

    private void RefreshMaxBox()
    {
        if (maxNumberBoxes.Count > 0)
        {
            foreach (Box box in maxNumberBoxes)
            {
                box.Highlight(false);
            }
        }

        maxNumberBoxes = GetMaxNumberBoxes();
        foreach (Box box in maxNumberBoxes)
        {
            box.Highlight(true);
        }

        maxNumber.text = "MAX NUMBER " + maxNumberBoxes[0].num;
    }

    private List<Box> GetMaxNumberBoxes()
    {
        List<Box> resultBoxes = new List<Box>();
        Cell maxCell = new Cell(0, 0);

        for (int i = 0; i < MAX_BOX_COUNT; i++)
        {
            for (int j = 0; j < MAX_BOX_COUNT; j++)
            {
                if (IsGameBox(i, j) == false)
                {
                    continue;
                }

                if (boxes[maxCell.x, maxCell.y].num < boxes[i, j].num)
                {
                    maxCell = new Cell(i, j);
                }
            }
        }

        for (int i = 0; i < MAX_BOX_COUNT; i++)
        {
            for (int j = 0; j < MAX_BOX_COUNT; j++)
            {
                if (IsGameBox(i, j) == false)
                {
                    continue;
                }

                if (boxes[maxCell.x, maxCell.y].num == boxes[i, j].num)
                {
                    resultBoxes.Add(boxes[i, j]);
                }
            }
        }

        return resultBoxes;
    }

    private Box GetTouchedBox()
    {
        Box resultBox = null;

        eventDataCurrentPosition.position = Input.mousePosition;

        results.Clear();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        foreach (RaycastResult result in results)
        {
            Box box = result.gameObject.GetComponent<Box>();
            if (box != null && box.alive)
            {
                resultBox = box;
                break;
            }
        }
        return resultBox;
    }

    private bool IsTouchGameStart()
    {
        eventDataCurrentPosition.position = Input.mousePosition;

        results.Clear();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.name.Equals("CenterBox"))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateTime()
    {
        time.text = timeValue.ToString("F2");

        timeValue -= Time.deltaTime;
        if (timeValue < 0)
        {
            timeValue = 0.0f;
            gameStart = false;
            time.text = "GAME\nOVER";
        }
    }
}
