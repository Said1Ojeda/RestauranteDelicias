using System.Drawing;
using System.Windows.Forms;

namespace RestauranteDelicias.Pantallas
{
    // todo lo de administrar en un solo lugar, solo lo abre el admin
    public class FrmAjustes : Form
    {
        public const string USUARIOS = "usuarios";
        public const string BITACORA = "bitacora";
        public const string DATOS = "datos";
        public const string RESPALDO = "respaldo";
        public const string CONEXION = "conexion";

        public string Eleccion { get; private set; }

        readonly FlowLayoutPanel opciones = new FlowLayoutPanel();

        public FrmAjustes()
        {
            Estilo.Formulario(this, "Ajustes", 660, 570);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            opciones.Dock = DockStyle.Fill;
            opciones.Padding = new Padding(18);
            opciones.BackColor = Estilo.Crema;

            Opcion(USUARIOS, "Usuarios", "Meseros, cajeros y sus PIN", Icono.Usuarios, Estilo.Azul);
            Opcion(BITACORA, "Bitácora", "Quién anuló, descontó o cambió precios", Icono.Bitacora, Estilo.Naranja);
            Opcion(DATOS, "Datos del restaurante", "Nombre, NIT, propina, IVA y tiempos", Icono.Restaurante, Estilo.Vino);
            Opcion(RESPALDO, "Respaldo de la base", FrmRespaldo.Resumen(), Icono.Respaldo, Estilo.Libre);
            Opcion(CONEXION, "Conexión a la base", "Solo si el servidor cambió", Icono.Conexion, Estilo.Gris);

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, Padding = new Padding(12), FlowDirection = FlowDirection.RightToLeft, BackColor = Estilo.Crema };
            var cerrar = Estilo.Boton("Cerrar", Estilo.Gris);
            cerrar.Click += (s, e) => DialogResult = DialogResult.Cancel;
            botones.Controls.Add(cerrar);

            Controls.Add(opciones);
            Controls.Add(botones);
            Controls.Add(Estilo.Encabezado("Ajustes"));
            CancelButton = cerrar;
        }

        void Opcion(string clave, string titulo, string detalle, string icono, Color color)
        {
            var t = new Tarjeta
            {
                Titulo = titulo,
                Detalle = detalle,
                Icono = icono,
                Acento = color,
                Size = new Size(296, 128)
            };
            t.Click += (s, e) =>
            {
                Eleccion = clave;
                DialogResult = DialogResult.OK;
            };
            opciones.Controls.Add(t);
        }
    }
}
