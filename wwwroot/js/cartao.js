$(document).ready(function () {
    // Script para abrir o modal de adicionar cartão
    $('#btnAdicionarCartao').on('click', function () {
        $('#modalAdicionarCartao').modal('show');
    });

    // Script para excluir cartão
    $('.btnExcluirCartao').on('click', function () {
        var codCart = $(this).data('codcart');
        if (confirm('Você tem certeza que deseja excluir este cartão?')) {
            $.ajax({
                url: '/Conta/ExcluirCartao',
                type: 'POST',
                data: { codCart: codCart },
                success: function (response) {
                    location.reload(); // Recarrega a página após excluir
                },
                error: function () {
                    alert('Ocorreu um erro ao excluir o cartão.');
                }
            });
        }
    });
});
