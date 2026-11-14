using UnityEngine;
using TMPro;

// floating text above a unit, rises and fades out, then destroys itself
public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro _text;
    [SerializeField] private float _lifetime = 1f;
    [SerializeField] private float _riseSpeed = 1f;

    private float _timer;
    private Color _color;

    public void Setup(string text, Color color)
    {
        _text.text = text;
        _color = color;
        _text.color = color;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        transform.position += Vector3.up * _riseSpeed * Time.deltaTime;

        _color.a = 1f - _timer / _lifetime;
        _text.color = _color;

        if (_timer >= _lifetime) Destroy(gameObject);
    }
}
