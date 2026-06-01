// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

export function showPrompt(message) {
  return prompt(message, 'Type anything here');
}
// =============================================================================
// ADICIONE estas funções ao seu wwwroot/exampleJsInterop.js
// =============================================================================

// Mostrar/ocultar a senha na tela de login (ícones Bootstrap).
window.dllLoginAlternarSenha = function (botao) {
    var input = document.getElementById('dll-login-senha');
    if (!input) {
        return;
    }

    var icone = botao.querySelector('i');

    if (input.type === 'password') {
        input.type = 'text';
        if (icone) {
            icone.classList.remove('bi-eye');
            icone.classList.add('bi-eye-slash');
        }
        botao.setAttribute('aria-label', 'Ocultar senha');
    } else {
        input.type = 'password';
        if (icone) {
            icone.classList.remove('bi-eye-slash');
            icone.classList.add('bi-eye');
        }
        botao.setAttribute('aria-label', 'Mostrar senha');
    }
};

// Salva o tema atual num cookie para a tela de login (static SSR) conseguir lê-lo.
// Chame isto quando o MenuSistema disparar TemaChanged. Ex. (no host):
//   private async Task OnTemaChanged(TemaSistema t)
//   {
//       _tema = t;
//       await JS.InvokeVoidAsync("dllTemaSalvar", t == TemaSistema.Escuro ? "escuro" : "claro");
//   }
window.dllTemaSalvar = function (tema) {
    document.cookie = 'dll-tema=' + encodeURIComponent(tema) +
        ';path=/;max-age=31536000;samesite=lax';
};
