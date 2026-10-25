using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RestauranteDelicias
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // si algo truena que salga el mensaje y no se cierre todo con la mesa a medias
            Application.ThreadException += (s, e) =>
                MessageBox.Show(e.Exception.Message, "Algo salió mal", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Application.Run(new Pantallas.FrmPrincipal());
        }
    }
}
