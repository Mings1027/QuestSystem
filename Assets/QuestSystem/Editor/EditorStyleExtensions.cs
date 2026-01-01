using UnityEngine;
using UnityEngine.UIElements;

public static class QuestEditorTheme
{
    // --- Colors ---
    public static readonly Color HeaderBackground = new Color(0.2f, 0.2f, 0.2f);
    public static readonly Color BoxBackground = new Color(0.22f, 0.22f, 0.22f);
    public static readonly Color RowBackground = new Color(0.26f, 0.26f, 0.26f); // 조금 더 밝게
    public static readonly Color ItemHeaderBackground = new Color(0.28f, 0.28f, 0.28f);
    public static readonly Color ItemContentBackground = new Color(0.18f, 0.18f, 0.18f); // 더 어둡게 (깊이감)
    
    // Scene 섹션용 (약간 푸른빛이 도는 어두운 회색으로 씬 데이터임을 암시)
    public static readonly Color SceneSectionBackground = new Color(0.16f, 0.18f, 0.2f); 
    public static readonly Color SceneHeaderColor = new Color(0.6f, 0.7f, 0.8f); // 연한 하늘색 텍스트

    public static readonly Color BorderColor = new Color(0.15f, 0.15f, 0.15f);
    public static readonly Color SeparatorColor = new Color(0.15f, 0.15f, 0.15f);
    
    public static readonly Color CreateButtonColor = new Color(0.25f, 0.5f, 0.25f);
    public static readonly Color RemoveButtonColor = new Color(0.7f, 0.25f, 0.25f);
    
    public static readonly Color TextFieldBackground = new Color(0.14f, 0.14f, 0.14f);

    // --- Metrics ---
    public const float StandardRadius = 4f; // 5 -> 4 (조금 더 각지게)
    public const float SmallRadius = 2f;
    public const float BorderWidth = 1f;
}

public static class EditorStyleExtensions
{
    // -------------------------------------------------------------------------
    // Public Extensions
    // -------------------------------------------------------------------------

    public static void ApplyBoxStyle(this IStyle style, Color bgColor)
    {
        style.marginTop = 10;
        SetPadding(style, 5);
        SetBorder(style, QuestEditorTheme.BorderWidth, QuestEditorTheme.BorderColor);
        SetRadius(style, QuestEditorTheme.StandardRadius);
        style.backgroundColor = bgColor;
    }

    public static void ApplyRowStyle(this IStyle style)
    {
        style.flexDirection = FlexDirection.Row;
        style.marginBottom = 2;
        SetPadding(style, 3);
        SetRadius(style, QuestEditorTheme.SmallRadius);
        style.backgroundColor = QuestEditorTheme.RowBackground;
    }

    public static void ApplyQuestHeaderStyle(this IStyle style)
    {
        style.backgroundColor = QuestEditorTheme.BoxBackground;
        SetRadius(style, QuestEditorTheme.StandardRadius);
        SetPadding(style, 10);
        style.marginBottom = 15;
        SetBorder(style, 1, QuestEditorTheme.BorderColor);
    }

    public static void ApplyListItemHeaderStyle(this IStyle style)
    {
        style.flexDirection = FlexDirection.Row;
        style.alignItems = Align.Center;
        style.backgroundColor = QuestEditorTheme.ItemHeaderBackground;
        SetRadius(style, QuestEditorTheme.StandardRadius);
        SetPadding(style, 4, 4, 6, 6);
        style.marginBottom = 2;
        style.borderBottomWidth = 1;
        style.borderBottomColor = QuestEditorTheme.BorderColor;
    }

    public static void ApplyListItemContentStyle(this IStyle style)
    {
        style.backgroundColor = QuestEditorTheme.ItemContentBackground;
        style.borderBottomLeftRadius = QuestEditorTheme.StandardRadius;
        style.borderBottomRightRadius = QuestEditorTheme.StandardRadius;
        style.marginLeft = 12; // 들여쓰기 약간 증가
        style.marginRight = 4;
        SetPadding(style, 10);
        style.marginBottom = 8;
    }

    // [수정됨] Scene UI 타겟 섹션 스타일 (깔끔하게 변경)
    public static void ApplySceneSectionStyle(this IStyle style)
    {
        style.backgroundColor = QuestEditorTheme.SceneSectionBackground;
        SetRadius(style, QuestEditorTheme.StandardRadius);
        style.paddingTop = 8;
        style.paddingBottom = 8;
        style.paddingLeft = 10;
        style.paddingRight = 10;
        style.marginTop = 12;
        
        // 상단 구분선 효과
        style.borderTopWidth = 1;
        style.borderTopColor = new Color(1,1,1, 0.1f);
    }

    public static void ApplySeparatorStyle(this IStyle style)
    {
        style.marginBottom = 10;
        style.paddingBottom = 10;
        style.borderBottomWidth = 1;
        style.borderBottomColor = QuestEditorTheme.SeparatorColor;
    }

    // -------------------------------------------------------------------------
    // Element Specific Extensions
    // -------------------------------------------------------------------------

    public static void ApplyQuestTitleStyle(this TextField textField)
    {
        textField.style.marginBottom = 5;
        var input = textField.Q(className: "unity-text-field__input");
        if (input != null)
        {
            input.style.fontSize = 16; // 18 -> 16 (너무 크지 않게 조절)
            input.style.unityFontStyleAndWeight = FontStyle.Bold;
            input.style.paddingTop = 6;
            input.style.paddingBottom = 6;
            input.style.backgroundColor = QuestEditorTheme.TextFieldBackground;
            input.style.color = new Color(0.9f, 0.9f, 0.9f);
        }
    }

    public static void ApplyRemoveButtonStyle(this Button btn)
    {
        btn.text = "X";
        btn.style.width = 22;
        btn.style.backgroundColor = QuestEditorTheme.RemoveButtonColor;
        btn.style.alignSelf = Align.FlexStart;
        btn.style.color = Color.white;
        btn.style.unityFontStyleAndWeight = FontStyle.Bold;
    }

    public static void ApplyAddButtonStyle(this Button btn)
    {
        btn.text = "+ Add Item";
        btn.style.marginTop = 5;
        btn.style.height = 24;
    }

    public static void ApplyCustomButtonStyle(this Button btn, float height, int fontSize, bool isBold, Color bgColor)
    {
        btn.style.height = height;
        btn.style.fontSize = fontSize;
        if (isBold) btn.style.unityFontStyleAndWeight = FontStyle.Bold;
        btn.style.backgroundColor = bgColor;
        btn.style.marginTop = 5;
        btn.style.color = Color.white;
    }

    public static void ApplyCreateQuestButtonStyle(this Button btn)
    {
        btn.ApplyCustomButtonStyle(40, 14, true, QuestEditorTheme.CreateButtonColor);
        btn.style.marginTop = 10;
    }

    public static void ApplyStateBadgeStyle(this Label label)
    {
        label.style.position = Position.Absolute;
        label.style.right = 10;
        label.style.top = 8; // 위치 미세 조정
        label.style.paddingTop = 3;
        label.style.paddingBottom = 3;
        label.style.paddingLeft = 8;
        label.style.paddingRight = 8;
        label.style.borderTopLeftRadius = 10; // 캡슐 모양으로 변경
        label.style.borderTopRightRadius = 10;
        label.style.borderBottomLeftRadius = 10;
        label.style.borderBottomRightRadius = 10;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.color = Color.white;
        label.style.fontSize = 10; // 폰트 사이즈 조정
        label.style.opacity = 0.9f;
    }
    
    public static Color GetQuestStateColor(QuestState state)
    {
        switch (state)
        {
            case QuestState.Locked:     return new Color(0.3f, 0.3f, 0.3f); 
            case QuestState.CanStart:   return new Color(0.0f, 0.5f, 0.8f); 
            case QuestState.InProgress: return new Color(0.9f, 0.5f, 0.0f); 
            case QuestState.CanFinish:  return new Color(0.1f, 0.7f, 0.3f); 
            case QuestState.Finished:   return new Color(0.4f, 0.4f, 0.4f); 
            default:                    return Color.gray;
        }
    }

    // -------------------------------------------------------------------------
    // Private Helpers
    // -------------------------------------------------------------------------

    private static void SetPadding(IStyle style, float val)
    {
        style.paddingTop = style.paddingBottom = style.paddingLeft = style.paddingRight = val;
    }

    private static void SetPadding(IStyle style, float top, float bottom, float left, float right)
    {
        style.paddingTop = top; style.paddingBottom = bottom;
        style.paddingLeft = left; style.paddingRight = right;
    }

    private static void SetRadius(IStyle style, float val)
    {
        style.borderTopLeftRadius = style.borderTopRightRadius = 
        style.borderBottomLeftRadius = style.borderBottomRightRadius = val;
    }

    private static void SetBorder(IStyle style, float width, Color color)
    {
        style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = width;
        style.borderTopColor = style.borderBottomColor = style.borderLeftColor = style.borderRightColor = color;
    }
}