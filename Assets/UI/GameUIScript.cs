using UnityEngine;
using UnityEngine.UIElements;

public class GameUIScript : MonoBehaviour
{
    public PlayerScript PlayerScript;
    public UIDocument UIDoc;

    private Label m_HealthLabel;
    private VisualElement m_HealthBarMask;

    private void Start()
    {
        PlayerScript.OnHealthChange += HealthChanged;
        m_HealthLabel = UIDoc.rootVisualElement.Q<Label>("HealthLabel");
        m_HealthBarMask = UIDoc.rootVisualElement.Q<VisualElement>("HealthBarMask");

        HealthChanged();
    }


    void HealthChanged()
    {
        m_HealthLabel.text = $"{PlayerScript.CurrentHealth}/{PlayerScript.maxHealth}";
        float healthRatio = (float)PlayerScript.CurrentHealth / PlayerScript.maxHealth;
        float healthPercent = Mathf.Lerp(8, 88, healthRatio);
        m_HealthBarMask.style.width = Length.Percent(healthPercent);
    }
}