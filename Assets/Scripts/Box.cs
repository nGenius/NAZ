using UnityEngine;
using UnityEngine.UI;

public struct Cell
{
    public int x;
    public int y;

    public Cell(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class Box : MonoBehaviour
{
    public Text text;
    public int num { get; private set; }
    public bool alive { get; private set; }

    public Cell cell { get; private set; }

    void Awake()
    {
        text.text = "";
    }

    public void Reset()
    {
        num = Random.Range(1, 10);
        text.text = num.ToString();
        SetBackgroundColor(Color.white);
        alive = true;
    }

    public void ClearNumber()
    {
        text.text = "";
        text.color = Color.black;
    }

    public void Sum(Box source)
    {
        num += source.num;
        text.text = num.ToString();

        if (text.text.Contains("0"))
        {
            text.color = Color.gray;
            SetBackgroundColor(Color.black);
            num = 0;
            alive = false;
        }
    }

    public void SetBackgroundColor(Color color)
    {
        GetComponent<Image>().color = color;
    }

    public void AbsoluteSum(Box source)
    {
        num += source.num;
        text.text = num.ToString();
    }

    public void SetCell(int x, int y)
    {
        cell = new Cell(x, y);
    }

    public bool IsNearBox(Box other)
    {
        return (Mathf.Abs(cell.x - other.cell.x) == 1 && Mathf.Abs(cell.y - other.cell.y) == 0)
            || (Mathf.Abs(cell.x - other.cell.x) == 0 && Mathf.Abs(cell.y - other.cell.y) == 1);
    }

    public void Highlight(bool active)
    {
        if (alive == false)
        {
            return;
        }

        text.color = active ? Color.red : Color.black;
    }
}