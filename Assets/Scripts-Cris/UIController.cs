using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD: muestra angulo del barco, estado del ancla y cooldown del canon.
/// Asigna los Text/Image desde el Inspector.
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("Referencias")]
    public SerialController serial;
    public BarcoController  barco;

    [Header("Textos HUD")]
    public Text  txtAngulo;
    public Text  txtAncla;
    public Text  txtCanon;
    public Text  txtPot;

    [Header("Indicadores")]
    public Image panelAncla;     // se pone rojo cuando esta anclado
    public Image panelCanon;     // parpadea al disparar
    public Color colorActivo   = Color.red;
    public Color colorInactivo = new Color(1, 1, 1, 0.3f);

    private CanonController _canon;
    private AnclaController _ancla;
    private float _canonFlash = 0f;

    void Start()
    {
        if (barco != null)
        {
            _canon = barco.GetComponent<CanonController>();
            _ancla = barco.GetComponent<AnclaController>();
        }
    }

    void Update()
    {
        // Angulo
        if (txtAngulo != null)
            txtAngulo.text = $"Rumbo: {barco.transform.eulerAngles.y:F0}°";

        // Potenciometro raw
        if (txtPot != null && serial != null)
            txtPot.text = $"POT: {serial.potValue}";

        // Ancla
        bool anclada = _ancla != null && _ancla.EstaAnclado;
        if (txtAncla != null)
            txtAncla.text = anclada ? "ANCLA: BAJADA" : "ANCLA: SUBIDA";
        if (panelAncla != null)
            panelAncla.color = anclada ? colorActivo : colorInactivo;

        // Canon flash
        if (serial != null && serial.canonFired) _canonFlash = 0.3f;
        if (_canonFlash > 0f)
        {
            _canonFlash -= Time.deltaTime;
            if (panelCanon != null) panelCanon.color = colorActivo;
            if (txtCanon   != null) txtCanon.text    = "CANON: DISPARADO!";
        }
        else
        {
            if (panelCanon != null) panelCanon.color = colorInactivo;
            if (txtCanon   != null) txtCanon.text    = "CANON: LISTO";
        }
    }
}
