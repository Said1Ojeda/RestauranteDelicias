using System;

namespace RestauranteDelicias
{
    // error que se le puede mostrar tal cual al usuario, no es un bug
    public class AvisoException : Exception
    {
        public AvisoException(string mensaje) : base(mensaje) { }
    }
}
