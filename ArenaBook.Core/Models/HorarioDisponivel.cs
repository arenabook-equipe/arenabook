using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaBook.Core.Models
{
    //cada sugestão precisa transportar início e fim.
    //representa apenas o resultado
    public record HorarioDisponivel(TimeOnly HoraInicio, TimeOnly HoraFim);
}
