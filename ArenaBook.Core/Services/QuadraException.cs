namespace ArenaBook.Core.Services;

public class QuadraInvalidaException(string mensagem) : Exception(mensagem);

public class QuadraNaoEncontradaException(string mensagem) : Exception(mensagem);