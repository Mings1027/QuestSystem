using UnityEngine;
using UnityEngine.UIElements;

public static class EditorStyleExtensions
{
    // 박스 스타일 (컨테이너)
    public static void ApplyBoxStyle(this IStyle style, Color bgColor)
    {
        style.marginTop = 10;
        style.paddingTop = 5; style.paddingBottom = 5; style.paddingLeft = 5; style.paddingRight = 5;
        style.borderTopWidth = 1; style.borderBottomWidth = 1; style.borderLeftWidth = 1; style.borderRightWidth = 1;
        style.borderTopColor = new Color(0.4f, 0.4f, 0.4f); style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
        style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f); style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
        style.borderTopLeftRadius = 5; style.borderTopRightRadius = 5; 
        style.borderBottomLeftRadius = 5; style.borderBottomRightRadius = 5;
        style.backgroundColor = bgColor;
    }

    // 리스트 아이템 스타일 (한 줄)
    public static void ApplyRowStyle(this IStyle style)
    {
        style.flexDirection = FlexDirection.Row;
        style.marginBottom = 2;
        style.paddingTop = 3; style.paddingBottom = 3; style.paddingLeft = 3; style.paddingRight = 3;
        style.borderTopLeftRadius = 3; style.borderTopRightRadius = 3; 
        style.borderBottomLeftRadius = 3; style.borderBottomRightRadius = 3;
        style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
    }

    // X 버튼 스타일
    public static void ApplyRemoveButtonStyle(this Button btn)
    {
        btn.text = "X";
        btn.style.width = 25;
        btn.style.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
        btn.style.alignSelf = Align.FlexStart;
    }

    // + 추가 버튼 스타일
    public static void ApplyAddButtonStyle(this Button btn)
    {
        btn.text = "+ Add Item";
        btn.style.marginTop = 5;
        btn.style.height = 25;
    }
}