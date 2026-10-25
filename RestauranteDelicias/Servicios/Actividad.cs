using System;
using System.Windows.Forms;

namespace RestauranteDelicias.Servicios
{
    // se fija en cada tecla y cada clic del programa, asi se sabe cuanto lleva la compu sin que nadie la toque
    public class Actividad : IMessageFilter
    {
        const int WM_KEYDOWN = 0x0100;
        const int WM_SYSKEYDOWN = 0x0104;
        const int WM_MOUSEMOVE = 0x0200;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_RBUTTONDOWN = 0x0204;
        const int WM_MOUSEWHEEL = 0x020A;

        static DateTime ultima = DateTime.Now;

        public static void Empezar()
        {
            Application.AddMessageFilter(new Actividad());
        }

        public static void Tocar()
        {
            ultima = DateTime.Now;
        }

        public static TimeSpan SinUso
        {
            get { return DateTime.Now - ultima; }
        }

        public bool PreFilterMessage(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_KEYDOWN:
                case WM_SYSKEYDOWN:
                case WM_MOUSEMOVE:
                case WM_LBUTTONDOWN:
                case WM_RBUTTONDOWN:
                case WM_MOUSEWHEEL:
                    ultima = DateTime.Now;
                    break;
            }
            return false;
        }
    }
}
