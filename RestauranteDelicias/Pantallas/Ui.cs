using System;
using System.Windows.Forms;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    public static class Ui
    {
        // corre algo que toca la base y si falla muestra un mensaje entendible en vez de cerrar el programa
        public static bool Seguro(IWin32Window dueno, Action accion)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                accion();
                return true;
            }
            catch (AvisoException ex)
            {
                MessageBox.Show(dueno, ex.Message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("Ui.Seguro", ex);
                MessageBox.Show(dueno, Log.Amigable(ex), "No se pudo completar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        public static bool Confirmar(IWin32Window dueno, string texto, string titulo)
        {
            return MessageBox.Show(dueno, texto, titulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }
    }
}
