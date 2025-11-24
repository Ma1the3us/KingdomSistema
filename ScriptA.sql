-- CRIAÇÃO DO BANCO
drop  database if exists MeuProjetoMVC;
CREATE DATABASE IF NOT EXISTS MeuProjetoMVC
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_general_ci;

USE MeuProjetoMVC;


-- =====================================================
-- TABELA Usuario (Cliente e Admin)
-- =====================================================

select * from Entrega;
select * from Endereco_Entrega;
select * from Entrega_Produto;

CREATE TABLE Usuario (
    codUsuario INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    Senha VARCHAR(255) NOT NULL,
    Role ENUM('Admin','Funcionario','Cliente') NOT NULL DEFAULT 'Cliente',
    Telefone  varchar(15) not null,
    Ativo CHAR(1) NOT NULL DEFAULT '1',
    Foto longblob
);

-- =====================================================
-- TABELA Fornecedor
-- =====================================================
CREATE TABLE Fornecedor (
    codF INT AUTO_INCREMENT PRIMARY KEY,
    CNPJ VARCHAR(18) NOT NULL UNIQUE,
    Nome VARCHAR(100) NOT NULL
);

-- =====================================================
-- TABELA Categorias
-- =====================================================
CREATE TABLE Categorias (
    codCat INT AUTO_INCREMENT PRIMARY KEY,
    nomeCategoria VARCHAR(100) NOT NULL
);

-- =====================================================
-- TABELA Sub_Categoria
-- =====================================================
CREATE TABLE Sub_Categoria (
    codSub INT AUTO_INCREMENT PRIMARY KEY,
    nomeSubcategoria VARCHAR(100),
    codCat INT,
    FOREIGN KEY (codCat) REFERENCES Categorias(codCat)
);

-- =====================================================
-- TABELA Produto
-- =====================================================
CREATE TABLE Produto (
    codProd INT AUTO_INCREMENT PRIMARY KEY,
    nomeProduto VARCHAR(200),
    Descricao VARCHAR(255) NOT NULL DEFAULT '',
    Quantidade INT NOT NULL DEFAULT 1,
    quantidadeTotal INT NOT NULL DEFAULT 1,
    Valor DECIMAL(10,2),
    Imagens longblob,
    codCat INT,
    codF INT,
    Desconto decimal(10,2),
    FOREIGN KEY (codCat) REFERENCES Categorias(codCat),
    FOREIGN KEY (codF) REFERENCES Fornecedor(codF) ON DELETE SET NULL ON UPDATE CASCADE
);

CREATE TABLE wishlist (
  codProd INT,
  codUsuario INT,
  PRIMARY KEY (codProd, codUsuario),
  FOREIGN KEY (codProd) REFERENCES Produto(codProd) ON DELETE CASCADE,
  FOREIGN KEY (codUsuario) REFERENCES Usuario(codUsuario) ON DELETE CASCADE
);

CREATE TABLE Avaliacao (
    codAvaliacao INT AUTO_INCREMENT PRIMARY KEY,
    codProd INT NOT NULL,
    codUsuario INT NOT NULL,
    nota INT NOT NULL CHECK (nota >= 1 AND nota <= 5),
    comentario TEXT,
    dataAvaliacao DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_avaliacao_produto FOREIGN KEY (codProd) REFERENCES Produto(codProd),
    CONSTRAINT fk_avaliacao_usuario FOREIGN KEY (codUsuario) REFERENCES Usuario(codUsuario),
    UNIQUE (codProd, codUsuario)  -- Garante que o usuário avalie o produto apenas uma vez
);


-- =====================================================
-- TABELA ProdutoMidia
-- =====================================================
CREATE TABLE ProdutoMidia (
    codMidia INT AUTO_INCREMENT PRIMARY KEY,
    codProd INT NOT NULL,
    tipoMidia ENUM('Imagem', 'Video') NOT NULL,
    midia longblob,
    Ordem int,
    FOREIGN KEY (codProd) REFERENCES Produto(codProd)
);

-- =====================================================
-- TABELA Item_Subcategoria
-- =====================================================
CREATE TABLE Item_Subcategoria (
    codSub INT,
    codProd INT,
    PRIMARY KEY (codSub, codProd),
    FOREIGN KEY (codSub) REFERENCES Sub_Categoria(codSub),
    FOREIGN KEY (codProd) REFERENCES Produto(codProd)
);

-- =====================================================
-- TABELA Cartao_Clie
-- =====================================================
CREATE TABLE Cartao_Clie (
    codCart INT AUTO_INCREMENT PRIMARY KEY,
    Numero VARCHAR(19) NOT NULL,
    digitos VARCHAR(4) NOT NULL,
    bandeira VARCHAR(255) NOT NULL,
    digitoSeguranca VARCHAR(4),
    dataVencimento varchar(5),
    tipoCart VARCHAR(10),
    codUsuario INT,
    FOREIGN KEY (codUsuario) REFERENCES Usuario(codUsuario)
);

select * from Usuario;
-- =====================================================
-- TABELA Endereco_Entrega
-- =====================================================
CREATE TABLE Endereco_Entrega (
    codEndereco INT PRIMARY KEY AUTO_INCREMENT PRIMARY KEY,
    Cep VARCHAR(10),
    Logradouro VARCHAR(100),
    Estado VARCHAR(100),
    Bairro VARCHAR(100),
    Cidade VARCHAR(100)
);

-- =====================================================
-- TABELA Venda
-- =====================================================
CREATE TABLE Venda (
    codVenda INT AUTO_INCREMENT PRIMARY KEY,
    codUsuario INT,
    valorTotalVenda decimal(10,2),
    formaPag enum ('Debito', 'Credito', 'Pix'),
    situacao enum ('Em andamento', 'Finalizada', 'Cancelada'),
    dataE DATE,
    FOREIGN KEY (codUsuario) REFERENCES Usuario(codUsuario)
);

-- =====================================================
-- TABELA Carrinho
-- =====================================================
-- CARRINHO / HISTÓRICO
CREATE TABLE Carrinho(
    codCarrinho INT AUTO_INCREMENT PRIMARY KEY,
    codVenda INT NOT NULL,
    dataCriacao DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (codVenda) REFERENCES Venda(codVenda)
);

-- =====================================================
-- TABELA ItemCarrinho
-- =====================================================
CREATE TABLE ItemCarrinho (
    codCarrinho INT NOT NULL,
    codProd INT NOT NULL,
    quantidade INT NOT NULL,
    valorProduto Decimal(10,2) NOT NULL,
    PRIMARY KEY (codCarrinho, codProd),
    FOREIGN KEY (codCarrinho) REFERENCES Carrinho(codCarrinho),
    FOREIGN KEY (codProd) REFERENCES Produto(codProd)
);

-- =====================================================
-- TABELA Comprovante
-- =====================================================
CREATE TABLE Comprovante (
    codComp INT AUTO_INCREMENT PRIMARY KEY,
    codVenda INT,
    dataHora DATETIME DEFAULT CURRENT_TIMESTAMP,
    valorTotal DOUBLE,
    codUsuario INT,
    FOREIGN KEY (codVenda) REFERENCES Venda(codVenda),
    FOREIGN KEY (codUsuario) REFERENCES Usuario(codUsuario)
);

-- =====================================================
-- TABELA Entrega
-- =====================================================
CREATE TABLE Entrega (
    codEntrega INT AUTO_INCREMENT PRIMARY KEY,
    codVenda INT,
    codUsuario INT,
    valorTotal double,
    codEnd INT,
    Numero VARCHAR(5),
    Complemento VARCHAR(50),
    TipoEndereco ENUM('Casa', 'Apartamento')  NULL,
    Andar VARCHAR(10),
    NomePredio VARCHAR(100),
    Situacao VARCHAR(150),
    dataInicial DATE,
    dataFinal DATE,
    nomeDestinatario varchar(100),
    emailDestinatario varchar(150),
    retirada enum ('Local','Entrega'),
    FOREIGN KEY (codVenda) REFERENCES Venda(codVenda),
    FOREIGN KEY (codUsuario) REFERENCES Usuario(codUsuario),
    FOREIGN KEY (codEnd) REFERENCES Endereco_Entrega(codEndereco)
);


/*
-- =============
-- Destinatário ==
-- =============
create table Destinatario(
codDestino int primary key auto_increment,
nomeDestinatario varchar(100) not null,
emailDestinatario varchar(100) not null,
codEntrega int
);
*/
-- =====================================================
-- TABELA Entrega_Produto
-- =====================================================
CREATE TABLE Entrega_Produto (
    codProd INT,
    nomeProduto VARCHAR(100) NOT NULL,
    Valor DOUBLE,
    Quantidade INT,
    Imagem  longblob,
    codEntrega INT,
    PRIMARY KEY (codEntrega, codProd),
    FOREIGN KEY (codEntrega) REFERENCES Entrega(codEntrega),
    FOREIGN KEY (codProd) REFERENCES Produto(codProd)
);

-- =====================================================
-- TABELA ZetaVersoes
-- =====================================================
CREATE TABLE ZetaVersoes (
    codZetaV INT AUTO_INCREMENT PRIMARY KEY,
    Pacote VARCHAR(100) NOT NULL,
    Valor DOUBLE NOT NULL,
    Descricao VARCHAR(255)
);

-- =====================================================
-- TABELA ZetaPass
-- =====================================================
CREATE TABLE ZetaPass (
    codZeta INT AUTO_INCREMENT PRIMARY KEY,
    dataInicial DATE,
    dataFinal DATE,
    codUsuario INT,
    codZetaV INT,
    formaPag enum ('Pix', 'Debito', 'Credito','Outro'),
    situacao varchar(1) default '1',
    FOREIGN KEY (codUsuario) REFERENCES Usuario(codUsuario),
    FOREIGN KEY (codZetaV) REFERENCES ZetaVersoes(codZetaV)
);

-- =====================================================
-- TABELA ZetaJogos
-- =====================================================
CREATE TABLE ZetaJogos (
    codZetaJ INT PRIMARY KEY AUTO_INCREMENT,
    nomeJogo VARCHAR(100) NOT NULL,
    Jogo LONGBLOB NOT NULL,
    jogoTipo VARCHAR(255) NOT NULL,
    codZetaV INT NOT NULL,
    imagemCapa  longblob NULL,
    classificacaoEtaria VARCHAR(10) NULL,
    categoria VARCHAR(100) NULL,
    FOREIGN KEY (codZetaV) REFERENCES ZetaVersoes(codZetaV)
);

-- ===================================
-- CUPONS!!!!
-- ===================================
CREATE TABLE  Cupom (
    codCupom INT AUTO_INCREMENT PRIMARY KEY,
    codUsuario INT NOT NULL,
    codigo VARCHAR(20) NOT NULL,
    desconto DOUBLE NOT NULL,
    dataCriacao DATETIME NOT NULL DEFAULT NOW(),
    dataValidade DATETIME NOT NULL,
    usado BOOLEAN NOT NULL DEFAULT FALSE,
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    FOREIGN KEY (codUsuario) REFERENCES Usuario(codUsuario),
    UNIQUE KEY (codUsuario, codigo) -- Um usuário não pode ter o mesmo código duplicado
);

-- =====================================================
-- TABELA de LOG para Debug
-- =====================================================
CREATE TABLE IF NOT EXISTS log_debug (
    id INT AUTO_INCREMENT PRIMARY KEY,
    mensagem VARCHAR(255),
    dataLog DATETIME
);


-- ===================================================
-- Criando as procedures de cadastrar
-- ===================================================

DELIMITER $$
drop procedure if exists cadastrar_usuario $$	
CREATE PROCEDURE cadastrar_usuario(
    IN p_nome VARCHAR(100),
    IN p_email VARCHAR(150),
    IN p_senha VARCHAR(255),
    IN p_telefone VARCHAR(15),
    in p_foto longblob,
    in p_role varchar(30)
)
BEGIN
    DECLARE v_codUsuario INT;

    -- Verifica se o e-mail já existe
    IF NOT EXISTS (SELECT 1 FROM Usuario WHERE Email = p_email) THEN

        -- Insere o usuário
        INSERT INTO Usuario (Nome, Email, Senha, Role, Telefone, Ativo, Foto)
        VALUES (p_nome, p_email, p_senha, p_role, p_telefone, '1', p_foto);


                SELECT 'Usuário cadastrado com sucesso!' AS Mensagem;

    ELSE
        SELECT 'Email já cadastrado.' AS Erro;
    END IF;
END $$

call cadastrar_usuario('Irineu', 'teste@Gmail.com', 'TESTE', '(11) 94444-3233', 'http/sadasa', 'Funcionario');

DELIMITER $$
drop procedure if exists cadastrar_usuario_cliente $$
CREATE PROCEDURE cadastrar_usuario_cliente(
    IN p_nome VARCHAR(100),
    IN p_email VARCHAR(150),
    IN p_senha VARCHAR(255),
    in p_telefone varchar(15),
    in f_foto varchar(255)
)
BEGIN

    -- Verifica se o e-mail já está cadastrado
    IF NOT EXISTS (SELECT 1 FROM Usuario WHERE Email = p_email) THEN

        -- Cadastra o usuário
        INSERT INTO Usuario (Nome, Email, Senha, Role, Telefone, Ativo, Foto)
        VALUES (p_nome, p_email, p_senha, 'Cliente', p_telefone,'1', f_foto);
        SELECT 'Usuário cadastrado com sucesso!' AS Mensagem;

    ELSE
        SELECT 'E-mail já cadastrado. Tente outro.' AS Erro;
    END IF;
END $$

call cadastrar_usuario_cliente('Irineu', 'cliente@Gmail.com', 'TESTE', '(11) 94444-3233', 'http/sadasa');

DELIMITER $$
drop procedure if exists cad_cart $$
CREATE PROCEDURE cad_cart(
    IN c_numero VARCHAR(19),
    IN c_digito VARCHAR(5),
    IN c_ultdigitos VARCHAR(4),
    IN c_bandeira VARCHAR(255),
    IN c_data VARCHAR(5),
    IN c_tipo VARCHAR(10),
    IN c_codusuario INT
)
BEGIN
    -- Verifica se o número do cartão já existe
    IF NOT EXISTS (
        SELECT 1 FROM Cartao_Clie WHERE Numero = c_numero
    ) THEN

        -- Insere o cartão vinculado ao codUsuario
        INSERT INTO Cartao_Clie (
            Numero,
            digitos,
            bandeira,
            digitoSeguranca,
            dataVencimento,
            tipoCart,
            codUsuario
        ) VALUES (
            c_numero,
            c_ultdigitos,
            c_bandeira,
            c_digito,
            c_data,
            c_tipo,
            c_codusuario
        );

        SELECT 'Cartão cadastrado com sucesso!' AS Mensagem;

    ELSE
        SELECT 'Erro: Cartão já cadastrado.' AS Erro;
    END IF;
END $$

call  cad_cart('400289221222','4232','2323','SALVE O CORITHIANS!!!!!!!!', '05/39','Debito', 2);


DELIMITER $$
drop procedure if exists cad_zeta_ver $$
CREATE PROCEDURE cad_zeta_ver(
    IN z_pacote VARCHAR(100),
    IN z_valor DOUBLE,
    IN z_descricao VARCHAR(255)
)
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM ZetaVersoes WHERE Pacote = z_pacote
    ) THEN
        INSERT INTO ZetaVersoes (Pacote, Valor, Descricao)
        VALUES (z_pacote, z_valor, z_descricao);
    ELSE
        SELECT 'Essa versão já foi cadastrada' AS Erro;
    END IF;
END $$

call cad_zeta_ver('DELUXE',10.20,'AQUI MAL É, AQUI MAL É');

-- TEM QUE TESTAR A FUNÇÃO !!!!! E INSERIR UMA VERIFICAÇÃO A CASO HAJA CARTÃO  CADASTRADO USAR

DELIMITER $$
drop procedure if exists cad_usuario_zeta $$
CREATE PROCEDURE cad_usuario_zeta(
    IN p_codUsuario INT,
    IN p_codZetaV INT,
	in z_formpag varchar(30)
)
BEGIN
    -- Verifica se o usuário está ativo
    IF EXISTS (
        SELECT 1 FROM Usuario WHERE codUsuario = p_codUsuario AND Ativo = '1'
    ) THEN

        -- Verifica se já existe passe zeta para o usuário
        IF NOT EXISTS (
            SELECT 1 FROM ZetaPass WHERE codUsuario = p_codUsuario
        ) THEN
            INSERT INTO ZetaPass (dataInicial, dataFinal,  codUsuario, codZetaV, formaPag)
            VALUES (NOW(), DATE_ADD(NOW(), INTERVAL 1 MONTH), p_codUsuario, p_codZetaV, z_formPag);

            SELECT 'Zeta Pass cadastrado com sucesso!' AS Mensagem;
        ELSE
            SELECT 'Usuário já possui um Zeta Pass ativo.' AS Erro;
        END IF;

    ELSE
        SELECT 'Usuário inativo ou não encontrado.' AS Erro;
    END IF;
END $$

call cad_usuario_zeta(2,1,'Debito');

select * from ZetaPass;

select * from log_debug;

select * from log_debug;



DELIMITER $$
drop procedure if exists cad_zeta_jog $$
CREATE PROCEDURE cad_zeta_jog(
    IN p_nomeJogo VARCHAR(100),
    IN p_jogo LONGBLOB,
    IN p_jogoTipo VARCHAR(255),
    IN p_codZetaV INT,
    IN p_imagemCapa  VARCHAR(255),
    IN p_classificacaoEtaria VARCHAR(10),
    IN p_categoria VARCHAR(100)
)
BEGIN
    -- Verifica se já existe o mesmo jogo (mesmo nome e tipo)
    IF NOT EXISTS (
        SELECT 1
        FROM ZetaJogos
        WHERE nomeJogo = p_nomeJogo AND jogoTipo = p_jogoTipo
    ) THEN
        -- Insere diretamente o jogo no catálogo Zeta com metadados extras
        INSERT INTO ZetaJogos (
            nomeJogo,
            Jogo,
            jogoTipo,
            codZetaV,
            imagemCapa,
            classificacaoEtaria,
            categoria
        ) VALUES (
            p_nomeJogo,
            p_jogo,
            p_jogoTipo,
            p_codZetaV,
            p_imagemCapa,
            p_classificacaoEtaria,
            p_categoria
        );

        SELECT 'Jogo Zeta cadastrado com sucesso!' AS Mensagem;
    ELSE
        SELECT 'Erro: Jogo com mesmo nome e tipo já cadastrado.' AS Erro;
    END IF;
END $$

call cad_zeta_jog('teste', 'dsadasdsa', 'rtx', '1', 'adasdasd', '12', 'HORROR!!!!!');

Delimiter $$
drop procedure if exists cad_Produto $$
create procedure cad_Produto(p_quantidade int, 
 p_imagens longblob, p_valor double, p_descricao varchar(255), p_nomeproduto varchar(100), 
 p_categorias int, p_codfornecedor int,p_desconto decimal(10,2), Out p_codProd int) -- Codigo do fornecedor, vai ser pego via select
begin

-- Declarando Variavéis
declare codC int;
declare codFo int;
declare quantTotal int;



 if not exists(select codProd from Produto where nomeProduto = p_nomeproduto)
						then
	   set codC = (select codCat from Categorias where codCat = p_categorias);
	   set codFo = (select codF from Fornecedor where codF = p_codfornecedor);
       set quantTotal = p_quantidade;
       
    insert into Produto( Quantidade, quantidadeTotal, Imagens, Valor, Descricao, nomeProduto, Desconto,codCat,codF)
				values( p_quantidade, quantTotal, p_imagens, p_valor, p_descricao, p_nomeproduto, p_desconto ,codC, codFo);
	
    set p_codProd = last_insert_id();
		select 'Produto cadastrado com sucesso!' as Sucesso;
  Else
		select 'Produto já cadastrado ou erro ao cadastrar' as Erro;
   
   end if;
end $$


CALL  cad_produto(2, 'asasd', 12.20, 'ADSDSDSD', 'DSDS', 1, 1, 30);

DELIMITER $$
drop procedure if exists cad_midia_prod $$
CREATE  PROCEDURE cad_midia_prod(
    IN p_midia  LONGBLOB,
    IN p_cod INT,
    IN p_tipomidia VARCHAR(10)
)
BEGIN
    DECLARE codP INT;
    DECLARE proxOrdem INT;

    -- Verifica se o produto existe
    SELECT codProd INTO codP FROM Produto WHERE codProd = p_cod;

    IF codP IS NULL THEN
        SELECT 'Produto não encontrado.' AS Erro;
    ELSE
        -- Verifica se a mídia já existe para esse produto
        IF NOT EXISTS (
            SELECT 1 FROM ProdutoMidia
            WHERE midia = p_midia 
              AND tipoMidia = p_tipomidia 
              AND codProd = codP
        ) THEN
            -- Define a próxima ordem (pega o maior valor atual + 1)
            SELECT IFNULL(MAX(ordem), 0) + 1 INTO proxOrdem
            FROM ProdutoMidia
            WHERE codProd = codP;

            -- Insere a nova mídia com a ordem calculada
            INSERT INTO ProdutoMidia (midia, codProd, tipoMidia, ordem)
            VALUES (p_midia, codP, p_tipomidia, proxOrdem);

            SELECT CONCAT('Mídia cadastrada com sucesso! Ordem = ', proxOrdem) AS Sucesso;
        ELSE
            SELECT 'Mídia já cadastrada para esse produto.' AS Erro;
        END IF;
    END IF;
END
call cad_midia_prod('asdsdasd',1,'Video');


Delimiter $$
drop procedure if exists cad_categoria $$
create procedure cad_categoria (p_nome varchar(100))
begin

-- Declarando variavel
declare Erro varchar(100);		
        
        if not exists( select codCat from Categorias where nomeCategoria = p_nome)
				then
                insert into Categorias(nomeCategoria)
					values(p_nome);
		-- Caso haja erro no cadastro
        Else
			select Erro = 'Erro ao cadastrar a categoria';
    end if;
End $$

call cad_categoria('teste');

Delimiter $$
drop procedure if exists cad_subcat $$
create procedure cad_subcat (p_nomesub varchar(100), p_cat int) -- o p_cat, é o código da categoria, para essa parte é melhor fazer um select, selecionando  a categoria pelo nome, e pegando o código dele usando um select, como na ativade do professor
begin

-- declarando variaveis


		if not exists (select codSub from Sub_Categoria where nomeSubcategoria = p_nomesub && codCat = p_cat)
			then
            insert into Sub_Categoria (nomeSubcategoria, codCat)
				values(p_nomesub, p_cat);
                
          Else
			select  'Erro ao cadastrar uma subcategoria' as Erro;
		end if;
        
end $$


call cad_subcat('teste2',1);

Delimiter $$ 
drop procedure if exists cad_itemSub $$
create procedure cad_itemSub (p_cod int, p_codSub int)
begin

-- Declarando Variavel
Declare codP int;
declare codS int;


set codP = (select codProd from Produto where codProd = p_cod);
set codS = (select codSub from Sub_Categoria where codSub = p_codSub);

	if not exists (select codProd from Item_Subcategoria where codSub = codS)
										then
	insert into Item_Subcategoria(codSub, codProd)
				values(codS, codP);
	Else
			select  'Erro ao associar o item a subcategoria' as Erro;
	End if;
End $$

call cad_itemSub(1,1);




DELIMITER $$
drop procedure if exists cad_fornecedor $$
CREATE PROCEDURE cad_fornecedor (
    IN f_cnpj BIGINT,
    IN f_nome VARCHAR(100)
)
BEGIN
    -- Verifica se o fornecedor com o mesmo CNPJ já existe
    IF NOT EXISTS (
        SELECT 1 FROM Fornecedor WHERE CNPJ = f_cnpj
    ) THEN
        INSERT INTO Fornecedor (CNPJ, Nome)
        VALUES (f_cnpj, f_nome);
        
        SELECT 'Fornecedor cadastrado com sucesso!' AS Sucesso;
    ELSE
        SELECT 'Fornecedor com esse CNPJ já está cadastrado.' AS Erro;
    END IF;
END $$


call cad_fornecedor(1212131,'Delimiter');


DELIMITER $$
DROP PROCEDURE IF EXISTS cad_carrinho $$
CREATE PROCEDURE cad_carrinho(
    IN p_cod INT,                -- Produto
    IN ca_quantidade INT,        -- Quantidade
    IN c_codUsuario INT          -- Usuário
)
BEGIN
    DECLARE codP INT;
    DECLARE codCa INT;
    DECLARE codV INT;
    DECLARE pQuantidade INT;
    DECLARE pValor DOUBLE;
    DECLARE prDesconto DOUBLE;
    DECLARE vP DOUBLE;
    DECLARE vT DOUBLE;

    SET sql_safe_updates = 0;

    -- ===========================
    -- 1) VERIFICA SE O USUÁRIO EXISTE
    -- ===========================
    IF NOT EXISTS (SELECT 1 FROM Usuario WHERE codUsuario = c_codUsuario) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Usuário não encontrado.';
    END IF;

    -- ===========================
    -- 2) VERIFICA SE EXISTE VENDA EM ANDAMENTO
    -- ===========================
    SET codV = (
        SELECT codVenda 
        FROM Venda 
        WHERE codUsuario = c_codUsuario 
        AND situacao = 'Em andamento'
        LIMIT 1
    );

    -- ===========================
    -- SE NÃO EXISTIR VENDA EM ANDAMENTO → CRIA VENDA NOVA + CARRINHO NOVO
    -- ===========================
    IF codV IS NULL THEN
        INSERT INTO Venda (codUsuario, situacao, dataE)
        VALUES (c_codUsuario, 'Em andamento', NOW());
        
        SET codV = LAST_INSERT_ID();

        INSERT INTO Carrinho (codVenda, dataCriacao)
        VALUES (codV, NOW());

        SET codCa = LAST_INSERT_ID();
    ELSE
        -- SE EXISTE VENDA EM ANDAMENTO → PEGAR O CARRINHO DELA
        SET codCa = (
            SELECT codCarrinho 
            FROM Carrinho 
            WHERE codVenda = codV
            LIMIT 1
        );

        -- Caso raro: venda existe mas carrinho não
        IF codCa IS NULL THEN
            INSERT INTO Carrinho (codVenda, dataCriacao)
            VALUES (codV, NOW());

            SET codCa = LAST_INSERT_ID();
        END IF;
    END IF;

    -- ===========================
    -- 3) BUSCA DADOS DO PRODUTO
    -- ===========================
    SET codP = p_cod;

    SET pQuantidade = (SELECT Quantidade FROM Produto WHERE codProd = codP);
    SET pValor = (SELECT Valor FROM Produto WHERE codProd = codP);
    SET prDesconto = (SELECT Desconto FROM Produto WHERE codProd = codP);

    IF pQuantidade IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Produto não encontrado.';
    END IF;

    -- ESTOQUE INSUFICIENTE
    IF pQuantidade < ca_quantidade THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Quantidade insuficiente em estoque.';
    END IF;

    -- ===========================
    -- 4) CALCULA VALOR DO ITEM
    -- ===========================
    IF prDesconto IS NOT NULL THEN
        SET vP = (pValor * (1 - prDesconto / 100)) * ca_quantidade;
    ELSE
        SET vP = pValor * ca_quantidade;
    END IF;

    -- ===========================
    -- 5) INSERE OU ATUALIZA ITEM
    -- ===========================
    IF NOT EXISTS (
        SELECT 1 FROM ItemCarrinho 
        WHERE codCarrinho = codCa AND codProd = codP
    ) THEN
        INSERT INTO ItemCarrinho (codCarrinho, codProd, quantidade, valorProduto)
        VALUES (codCa, codP, ca_quantidade, vP);
    ELSE
        UPDATE ItemCarrinho
        SET quantidade = quantidade + ca_quantidade,
            valorProduto = valorProduto + vP
        WHERE codCarrinho = codCa AND codProd = codP;
    END IF;

    -- ===========================
    -- 6) ATUALIZA ESTOQUE
    -- ===========================
    UPDATE Produto
    SET Quantidade = Quantidade - ca_quantidade
    WHERE codProd = codP;

    -- ===========================
    -- 7) ATUALIZA VALOR TOTAL DA VENDA
    -- ===========================
    SET vT = (
        SELECT IFNULL(SUM(valorProduto), 0)
        FROM ItemCarrinho
        WHERE codCarrinho = codCa
    );

    UPDATE Venda
    SET valorTotalVenda = vT
    WHERE codVenda = codV;

    SET sql_safe_updates = 1;

END $$
DELIMITER ;

select * from Carrinho;
select * from Venda;
select * from ItemCarrinho;
select * from Produto;


-- call cad_carrinho(1,2,2);

DELIMITER $$
drop procedure if exists inserir_favorito $$
CREATE PROCEDURE inserir_favorito (
    IN p_codProd INT,
    IN p_codUsuario INT
)
BEGIN
    -- Verifica se o favorito já existe para evitar duplicata
    IF NOT EXISTS (
        SELECT 1 FROM wishlist WHERE codProd = p_codProd AND codUsuario = p_codUsuario
    ) THEN
        INSERT INTO wishlist (codProd, codUsuario) VALUES (p_codProd, p_codUsuario);
        SELECT 'Produto adicionado aos favoritos.' AS Sucesso;
    ELSE
        SELECT 'Produto já está nos favoritos.' AS Info;
    END IF;
END $$

select * from ItemCarrinho;
call inserir_favorito(1,2);

DELIMITER $$
drop procedure if exists inserir_avaliacao $$
CREATE PROCEDURE inserir_avaliacao (
    IN p_codProd INT,
    IN p_codUsuario INT,
    IN p_nota INT,
    IN p_comentario TEXT
)
BEGIN
    -- Verifica se já existe avaliação para esse produto e usuário
    IF EXISTS (
        SELECT 1 FROM Avaliacao WHERE codProd = p_codProd AND codUsuario = p_codUsuario
    ) THEN
        SELECT 'Usuário já avaliou esse produto.' AS Erro;
    ELSE
        INSERT INTO Avaliacao (codProd, codUsuario, nota, comentario)
        VALUES (p_codProd, p_codUsuario, p_nota, p_comentario);
        SELECT 'Avaliação inserida com sucesso!' AS Sucesso;
    END IF;
END $$

call inserir_avaliacao(1,2,5,'QUERO CAFÉ!!!!!!');



/* SE FOR USAR A TABELA DE DESTINATÁRIO SEPARADO!!!!!!!!!!!!!!! 
   DA TABELA DE ENTREGA!!!!!!!!
Delimiter $$
drop procedure if exists cad_destinatario $$
create procedure cad_destinatario(e_cod int, d_nome varchar(100), d_email varchar(100))
begin
	
    declare nomeU varchar(100);
    declare emailU varchar(150);
	
	if exists (select codUsuario from Usuario where Email = d_email)
							then 
	select Nome, Email into nomeU, emailU
    from Usuario
    where Email = d_email;
	
    insert into Destinatario(nomeDestinatario, emailDestinatario,codEntrega)
				values(nomeU,emailU, e_cod);
                select 'Destinatário cadastrado com sucesso' as Mensagem;
    
    else
			insert into Destinatario(nomeDestinatario, emailDestinatario, codEntrga)
							values(d_nome, d_email, e_cod);
			select 'Destinatário cadastrado com sucesso' as Mensagem;
            
	end if;
    
end $$
*/

describe Destinatario;
-- =========================
-- ALTERAR TABELAS
-- =========================
DELIMITER $$
drop procedure if exists alterar_usuario $$
CREATE PROCEDURE alterar_usuario(
    IN p_codUsuario INT,
    IN p_nome VARCHAR(100),
    IN p_email VARCHAR(150),
    IN p_senha VARCHAR(100),
    In p_telefone varchar(15),
    in p_foto longblob
)
BEGIN
    UPDATE Usuario
    SET Nome = p_nome,
        Email = p_email,
        Senha = p_senha,
	Telefone= p_telefone,
		Foto = p_foto
    WHERE codUsuario = p_codUsuario;
    
    SELECT 'Usuário atualizado com sucesso!' AS Sucesso;
END $$

call alterar_usuario_cliente(2,'Alexandro','adsad@gmail.com', 'Aulas', '(14) 12312-123', 'asdas');

DELIMITER $$
drop procedure if exists atualizar_usuario_adm $$
CREATE PROCEDURE atualizar_usuario_adm(
    IN p_codUsuario INT,
    IN p_nome VARCHAR(100),
    IN p_email VARCHAR(150),
    IN p_senha VARCHAR(255),
    IN p_role VARCHAR(50),
    in p_telefone varchar(15),
    in p_foto longblob,
    in p_ativo varchar(1)
)
BEGIN
  

    -- Atualiza os dados do usuário incluindo a role
    UPDATE Usuario
    SET Nome = p_nome,
        Email = p_email,
        Senha = p_senha,
        role = p_role,
	Telefone = p_telefone,
		Foto = p_foto,
        Ativo = p_ativo
    WHERE codUsuario = p_codUsuario;

   
END $$

call atualizar_usuario_adm(1,'asdasd','email@gmail','12321','Admin','(11) 92323-1232', 'asdad');

DELIMITER $$
drop procedure if exists editar_produto $$
CREATE PROCEDURE editar_produto (
    IN p_cod INT,
    IN p_quant INT,
    IN p_quanttotal INT,
    IN p_valor DOUBLE,
    IN p_nome VARCHAR(100),
    IN p_descricao VARCHAR(255),
    IN p_cat INT,
    IN p_for INT,
    in p_desconto decimal(10,2),
    in p_imagem longblob
)
BEGIN
    IF EXISTS (SELECT 1 FROM Produto WHERE codProd = p_cod) THEN
        UPDATE Produto
        SET Quantidade = p_quant,
            quantidadeTotal = p_quanttotal,
            Valor = p_valor,
            Descricao = p_descricao,
            nomeProduto = p_nome,
            codCat = p_cat,
            codF = p_for,
            Desconto = p_desconto,
            Imagens = p_imagem
        WHERE codProd = p_cod;

        SELECT 'Produto atualizado com sucesso.' AS Mensagem;
    ELSE
        SELECT 'Produto não encontrado.' AS Mensagem;
    END IF;
END $$

call editar_produto(1,221,1223,20,'teste','OLHA O PASQUAL!!!!',1,1,15);

Delimiter $$
drop procedure if exists editar_fornecedor $$
create procedure editar_fornecedor( f_cnpj varchar(18), f_nome varchar(100), f_cod int)
begin
	Update Fornecedor
    set nome = f_nome, CNPJ = f_cnpj
    where codF = f_cod;
end $$

/*
DELIMITER $$ 
drop procedure if exists editar_imagem_principal_prod $$
CREATE PROCEDURE editar_imagem_principal_prod (
    IN p_cod INT, 
    IN p_imagem  VARCHAR(255)
)
BEGIN
    IF EXISTS (SELECT 1 FROM Produto WHERE codProd = p_cod) THEN
        UPDATE Produto
        SET Imagens = p_imagem
        WHERE codProd = p_cod;

        SELECT 'Imagem principal do produto atualizada com sucesso.' AS Mensagem;
    ELSE
        SELECT 'Produto não encontrado.' AS Mensagem;
    END IF;
END $$

call editar_imagem_principal_prod(1, 'ADASD');
*/
DELIMITER $$
drop procedure if exists editar_carrinho $$
CREATE PROCEDURE editar_carrinho (
    IN p_cod INT,             -- Código do produto
    IN ca_quantidade INT,     -- Quantidade desejada (nova)
    IN u_cod INT              -- Código do usuário
)
BEGIN
	Declare codV int;
	Declare vT double;
    DECLARE pQuantidadeEstoque INT;
    DECLARE pQuantidadeAtual INT;
    DECLARE pValorUnidade DOUBLE;

	set codV = (select codVenda from Venda where codUsuario = u_cod and situacao = 'Em andamento');
    
    -- Validar usuário
    IF NOT EXISTS (SELECT 1 FROM Usuario WHERE codUsuario = u_cod) THEN
        SELECT 'Usuário não encontrado.' AS Erro;
    END IF;

    -- Obter quantidade em estoque do produto
    SET pQuantidadeEstoque = (SELECT Quantidade FROM Produto WHERE codProd = p_cod);
	


    -- Obter quantidade atual no carrinho (ItemCarrinho) para esse usuário e venda
    SET pQuantidadeAtual = (
        SELECT quantidade 
        FROM ItemCarrinho ic
        join Carrinho c on ic.codCarrinho = c.codCarrinho
        JOIN Venda v ON c.codVenda = v.codVenda
        WHERE ic.codProd = p_cod AND v.codUsuario = u_cod AND v.codVenda = codV
        LIMIT 1
    );

    IF pQuantidadeAtual IS NULL THEN
        SELECT 'Produto não está no carrinho.' AS Erro;
    END IF;

    -- Obter valor unitário do produto (valorProduto / quantidade)
    SET pValorUnidade = (
        SELECT valorProduto / quantidade 
        FROM ItemCarrinho ic
        join Carrinho c on ic.codCarrinho = c.codCarrinho
		JOIN Venda v ON c.codVenda = v.codVenda
        WHERE ic.codProd = p_cod AND v.codUsuario = u_cod AND v.codVenda = codV
        LIMIT 1
    );

    -- Validar quantidade desejada
    IF ca_quantidade <= 0 THEN
        SELECT 'Quantidade deve ser maior que zero.' AS Erro;
        
    END IF;



    -- Atualizar quantidade e valor no ItemCarrinho
    UPDATE ItemCarrinho ic
    join Carrinho c on ic.codCarrinho = c.codCarrinho
    JOIN Venda v ON c.codVenda = v.codVenda
    SET ic.quantidade = ca_quantidade,
        ic.valorProduto = pValorUnidade * ca_quantidade
    WHERE ic.codProd = p_cod AND v.codUsuario = u_cod AND v.codVenda = codV;

    -- Atualizar estoque no Produto, se não for jogo
    
     SET vT = (
        SELECT COALESCE(SUM(valorProduto), 0)
        FROM ItemCarrinho
        WHERE codCarrinho = (SELECT codCarrinho FROM Carrinho WHERE codVenda = codV LIMIT 1)
    );

    -- Atualiza o valor total da venda
    UPDATE Venda
    SET valorTotalVenda = vT
    WHERE codVenda = codV;

   
    SELECT 'Quantidade do carrinho atualizada com sucesso!' AS Sucesso;

   
END $$

call editar_carrinho(1,21,2);

/*
DELIMITER $$
drop procedure if exists alterar_promocao $$
CREATE PROCEDURE alterar_promocao (
    IN p_codProd INT,
    IN p_desconto DOUBLE
)
BEGIN
    IF EXISTS (SELECT 1 FROM Promocao WHERE codProd = p_codProd) THEN
        UPDATE Promocao
        SET Desconto = p_desconto
        WHERE codProd = p_codProd;
        SELECT 'Promoção atualizada com sucesso.' AS Sucesso;
    ELSE
        SELECT 'Promoção não encontrada para o produto informado.' AS Erro;
    END IF;
END $$

call alterar_promocao(1,21)
*/
select * from ProdutoMidia;

DELIMITER $$
drop procedure if exists alterar_produto_midia $$
CREATE PROCEDURE alterar_produto_midia (
    IN p_codProd INT,
    IN p_midia  longblob,
    IN p_tipoMidia ENUM('Imagem', 'Video')
)
BEGIN
        UPDATE ProdutoMidia
        SET midia = p_midia,
            tipoMidia = p_tipoMidia
        WHERE codProd = p_codProd AND tipoMidia = p_tipoMidia;
        SELECT CONCAT(p_tipoMidia, ' atualizada com sucesso.') AS Sucesso;
END $$

call alterar_produto_midia(1,'sasdsadasd','Imagem');

select * from Endereco_Entrega;
select * from Entrega;
select * from Entrega_Produto;


DELIMITER $$
DROP PROCEDURE IF EXISTS atualizar_dados_entrega_cliente $$
CREATE PROCEDURE atualizar_dados_entrega_cliente (
    IN u_cod INT,
    IN v_cod INT,
    IN p_cep VARCHAR(10),
    IN p_numero VARCHAR(10),
    IN p_complemento VARCHAR(50),
    IN p_tipoEndereco ENUM('Casa', 'Apartamento'),
    IN p_andar VARCHAR(40),
    IN p_nomePredio VARCHAR(100),
    IN p_logradouro VARCHAR(100),
    IN p_estado VARCHAR(100),
    IN p_bairro VARCHAR(100),
    IN p_cidade VARCHAR(100)
)
BEGIN
    DECLARE codE INT;
    DECLARE situacaoEntrega VARCHAR(30);
    DECLARE mensagemLog TEXT;
    DECLARE codEnderecoEntrega INT;

    -- Busca a entrega vinculada ao usuário e à venda
    SELECT codEntrega, Situacao
    INTO codE, situacaoEntrega
    FROM Entrega
    WHERE codUsuario = u_cod AND codVenda = v_cod
    LIMIT 1;

    -- Verifica se a entrega existe e está em preparação
    IF codE IS NOT NULL AND situacaoEntrega = 'Em andamento' THEN

        -- Verifica se endereço existe pelo CEP, logradouro, estado, bairro e cidade
        SELECT codEndereco INTO codEnderecoEntrega
        FROM Endereco_Entrega
        WHERE Cep = p_cep
          AND Logradouro = p_logradouro
          AND Estado = p_estado
          AND Bairro = p_bairro
          AND Cidade = p_cidade
        LIMIT 1;

        -- Se não existe, cria novo endereço
        IF codEnderecoEntrega IS NULL THEN
            INSERT INTO Endereco_Entrega (Cep, Logradouro, Estado, Bairro, Cidade)
            VALUES (p_cep, p_logradouro, p_estado, p_bairro, p_cidade);

            SET codEnderecoEntrega = LAST_INSERT_ID();
        END IF;

        -- Atualiza a entrega com o codEndereco e os dados adicionais
        IF p_tipoEndereco = 'Apartamento' THEN
            UPDATE Entrega
            SET
                codEnd = codEnderecoEntrega,
                Numero = p_numero,
                Complemento = p_complemento,
                TipoEndereco = p_tipoEndereco,
                Andar = p_andar,
                NomePredio = p_nomePredio
            WHERE codEntrega = codE;

            SET mensagemLog = CONCAT('Entrega ', codE, ': Endereço atualizado como Apartamento.');

        ELSEIF p_tipoEndereco = 'Casa' THEN
            UPDATE Entrega
            SET
                codEnd = codEnderecoEntrega,
                Numero = p_numero,
                Complemento = p_complemento,
                TipoEndereco = p_tipoEndereco,
                Andar = NULL,
                NomePredio = NULL
            WHERE codEntrega = codE;

            SET mensagemLog = CONCAT('Entrega ', codE, ': Endereço atualizado como Casa.');

        ELSE
            UPDATE Entrega
            SET
                codEnd = codEnderecoEntrega,
                Numero = p_numero,
                Complemento = NULL,
                TipoEndereco = p_tipoEndereco,
                Andar = NULL,
                NomePredio = NULL
            WHERE codEntrega = codE;

            SET mensagemLog = CONCAT('Entrega ', codE, ': Endereço atualizado com tipo ', p_tipoEndereco, '.');
        END IF;

        INSERT INTO log_debug (mensagem, dataLog) VALUES (mensagemLog, NOW());

        SELECT 'Dados de entrega atualizados com sucesso!' AS Sucesso;

    ELSEIF codE IS NOT NULL AND situacaoEntrega <> 'Em preparação' THEN
        SET mensagemLog = CONCAT('Falha: Tentativa de atualizar entrega ', codE, ' já processada (situação: ', situacaoEntrega, ').');
        INSERT INTO log_debug (mensagem, dataLog) VALUES (mensagemLog, NOW());

        SELECT 'Erro: A entrega já foi processada e não pode ser alterada.' AS Erro;

    ELSE
        SET mensagemLog = CONCAT('Erro: Entrega não encontrada para usuario ', u_cod, ' e venda ', v_cod, '.');
        INSERT INTO log_debug (mensagem, dataLog) VALUES (mensagemLog, NOW());

        SELECT 'Erro: Entrega não encontrada para este usuário e venda.' AS Erro;
    END IF;

END $$

call atualizar_dados_entrega_cliente(2,1,'10','12','1asda','casa','12','sada','asda','asda','dazs','asd');


-- É UM INSERIR E EDITAR DESTINATÁRIO, JÁ QUE OS CAMPOS SÃO O MESMO! E na verdade vai acontecer na mesma situação
-- Já que na hora de incrementar a venda, ela não vai efetuar ou no caso, inserir diretamente os dados, vai ser necessário utilizar esse update para que funcione
-- (Nova versão)
DELIMITER $$ 
DROP PROCEDURE IF EXISTS atualizar_Destinatario $$
CREATE PROCEDURE atualizar_Destinatario(
    IN u_cod INT,               -- código do usuário
    IN v_cod INT,               -- código da venda
    IN e_nome VARCHAR(100),
    IN e_email VARCHAR(150)
)
BEGIN
    DECLARE codE INT;
    DECLARE situacaoEntrega VARCHAR(30);
    DECLARE mensagemLog TEXT;

    -- Busca a entrega vinculada ao usuário e à venda
    SELECT codEntrega, Situacao
    INTO codE, situacaoEntrega
    FROM Entrega
    WHERE codUsuario = u_cod AND codVenda = v_cod
    LIMIT 1;

    -- Verifica se a entrega existe
    IF codE IS NOT NULL THEN
        -- Atualiza os dados do destinatário
        UPDATE Entrega
        SET nomeDestinatario = e_nome,
            emailDestinatario = e_email
        WHERE codEntrega = codE;

        SET mensagemLog = CONCAT('Entrega ', codE, ': Destinatário atualizado com sucesso.');
        INSERT INTO log_debug (mensagem, dataLog) VALUES (mensagemLog, NOW());

        SELECT 'Destinatário atualizado com sucesso!' AS Sucesso;
    ELSE
        SET mensagemLog = CONCAT('Erro: Entrega não encontrada para usuario ', u_cod, ' e venda ', v_cod, '.');
        INSERT INTO log_debug (mensagem, dataLog) VALUES (mensagemLog, NOW());

        SELECT 'Erro: Entrega não encontrada para este usuário e venda.' AS Erro;
    END IF;

END $$

DELIMITER ;

-- call atualizar_Destinatario(1,'MARIO BIGOEDE', 'teste@gmail.com');

DELIMITER $$
drop procedure if exists atualizar_avaliacao $$
CREATE PROCEDURE atualizar_avaliacao (
    IN p_codProd INT,
    IN p_codUsuario INT,
    IN p_nota INT,
    IN p_comentario TEXT
)
BEGIN
    -- Verifica se a avaliação existe
    IF EXISTS (
        SELECT 1 FROM Avaliacao WHERE codProd = p_codProd AND codUsuario = p_codUsuario
    ) THEN
        UPDATE Avaliacao
        SET nota = p_nota,
            comentario = p_comentario,
            dataAvaliacao = NOW()
        WHERE codProd = p_codProd AND codUsuario = p_codUsuario;
        SELECT 'Avaliação atualizada com sucesso!' AS Sucesso;
    ELSE
        SELECT 'Avaliação não encontrada.' AS Erro;
    END IF;
END $$


call atualizar_avaliacao(1,2,5,'120');


DELIMITER $$

DROP PROCEDURE IF EXISTS inserir_formapagamento $$
CREATE PROCEDURE inserir_formapagamento(
    IN u_formaPag VARCHAR(30),
    IN u_codUsuario INT
)
BEGIN
    DECLARE codV INT;
    DECLARE cart INT;

    -- Busca a venda "Em andamento" do usuário
    SELECT codVenda INTO codV
    FROM Venda
    WHERE codUsuario = u_codUsuario AND situacao = 'Em andamento'
    LIMIT 1;

    IF codV IS NULL THEN
        SELECT ' Erro: Nenhuma venda em andamento encontrada para este usuário.' AS mensagem;
    ELSE
        -- Se o pagamento for PIX, atualiza direto
        IF u_formaPag = 'Pix' THEN
            UPDATE Venda
            SET formaPag = u_formaPag
            WHERE codVenda = codV;

            SELECT ' Forma de pagamento atualizada para PIX com sucesso.' AS mensagem;

        -- Se for Crédito ou Débito, checa se o cartão existe
        ELSEIF u_formaPag IN ('Credito', 'Debito') THEN
            SELECT codCart INTO cart
            FROM Cartao_Clie
            WHERE tipoCart = u_formaPag AND codUsuario = u_codUsuario
            LIMIT 1;

            IF cart IS NOT NULL THEN
                UPDATE Venda
                SET formaPag = u_formaPag
                WHERE codVenda = codV;

                SELECT CONCAT(' Forma de pagamento atualizada para ', u_formaPag, '.') AS mensagem;
            ELSE
                SELECT CONCAT('Nenhum cartão do tipo "', u_formaPag, '" encontrado para este usuário.') AS mensagem;
            END IF;

        ELSE
            SELECT ' Tipo de pagamento inválido. Use "Pix", "Credito" ou "Debito".' AS mensagem;
        END IF;
    END IF;
END $$

DELIMITER ;
-- call inserir_formaPagamento(2,'Pix');
-- =======================
-- PROCEDURES DE DELETE!!!
-- =======================

DELIMITER $$
DROP PROCEDURE IF EXISTS desativar_usuario $$
CREATE PROCEDURE desativar_usuario(p_codUsuario INT)
BEGIN
    -- Verifica se o usuário existe e está ativo
    IF EXISTS (SELECT 1 FROM Usuario WHERE codUsuario = p_codUsuario AND Ativo = 1) THEN

        -- Desativa o usuário
        UPDATE Usuario
        SET Ativo = 0
        WHERE codUsuario = p_codUsuario;

        -- Atualiza situação das vendas que não foram finalizadas para esse usuário
        UPDATE Venda
        SET situacao = 'Cancelada'
        WHERE codUsuario = p_codUsuario AND situacao <> 'Finalizada';

        SELECT 'Usuário desativado com sucesso.' AS Sucesso;

    ELSE
        SELECT 'Usuário não encontrado ou já desativado.' AS Erro;
    END IF;
END $$


call desativar_usuario(1);

DELIMITER $$
drop procedure if exists deletar_prod_carrinho $$
CREATE PROCEDURE deletar_prod_carrinho (
    IN p_cod INT,      -- código do produto
    IN u_cod INT      -- código do usuário
)
BEGIN
    DECLARE vT DOUBLE;
    DECLARE vCount INT;
	Declare codV int;
    
    set codV = (select codVenda from Venda where codUsuario = u_cod and situacao = 'Em andamento');
    
    -- Verifica se a venda (carrinho) pertence ao usuário
    SELECT COUNT(*) INTO vCount
    FROM Venda
    WHERE codVenda = codV AND codUsuario = u_cod;

    IF vCount = 0 THEN
        SELECT 'Venda não pertence ao usuário informado ou não existe.' AS Erro;
    END IF;

    -- Deleta o item do carrinho
    DELETE FROM ItemCarrinho
    WHERE codProd = p_cod 
      AND codCarrinho = (SELECT codCarrinho FROM Carrinho WHERE codVenda = codV LIMIT 1);

    -- Recalcula o total da venda/carrinho
    SET vT = (
        SELECT COALESCE(SUM(valorProduto), 0)
        FROM ItemCarrinho
        WHERE codCarrinho = (SELECT codCarrinho FROM Carrinho WHERE codVenda = codV LIMIT 1)
    );

    -- Atualiza o valor total da venda
    UPDATE Venda
    SET valorTotalVenda = vT
    WHERE codVenda = codV;

    SELECT 'Produto removido do carrinho e total atualizado.' AS Sucesso;
END $$

call deletar_prod_carrinho(1,2,1);

describe Venda;

-- ???
DELIMITER $$
DROP PROCEDURE IF EXISTS concluir_compra $$
CREATE PROCEDURE concluir_compra(
    IN u_cod INT,                 -- Código do usuário
    IN v_codigoCupom VARCHAR(20), -- Código do cupom (pode ser NULL)
    IN v_frete DOUBLE,            -- Valor do frete
    IN v_tipoRetirada ENUM('Local','Entrega') -- Tipo da retirada
)
BEGIN
    DECLARE vT DOUBLE DEFAULT 0;           -- Valor total da venda (produtos)
    DECLARE comP INT DEFAULT NULL;         -- Código do comprovante (se existir)
    DECLARE descontoCupom DOUBLE DEFAULT 0;
    DECLARE codCupom INT DEFAULT NULL;
    DECLARE codCarr INT DEFAULT NULL;
    DECLARE codV INT DEFAULT NULL;         -- Código da venda atual
    DECLARE codEn INT DEFAULT NULL;        -- Código da entrega

    -- Busca a venda em andamento do usuário
    SELECT codVenda INTO codV
    FROM Venda
    WHERE codUsuario = u_cod AND situacao = 'Em andamento'
    ORDER BY codVenda DESC
    LIMIT 1;

    IF codV IS NULL THEN
        SELECT 'Erro: Nenhuma venda em andamento encontrada para este usuário.' AS Erro;
        
       SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Erro: Nenhuma venda respectiva existe';
    END IF;

    -- Busca o carrinho vinculado à venda
    SELECT codCarrinho INTO codCarr
    FROM Carrinho
    WHERE codVenda = codV
    LIMIT 1;

    IF codCarr IS NULL THEN
        SELECT 'Erro: Carrinho não encontrado para esta venda.' AS Erro;
        
       SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Erro, carrinho não existente';
    END IF;

    -- Calcula o valor total dos produtos
    SELECT COALESCE(SUM(valorProduto * quantidade), 0)
    INTO vT
    FROM ItemCarrinho
    WHERE codCarrinho = codCarr;

    -- Verifica e aplica cupom (se houver)
    IF v_codigoCupom IS NOT NULL THEN
        SELECT codCupom, desconto INTO codCupom, descontoCupom
        FROM Cupom
        WHERE codigo = v_codigoCupom
          AND codUsuario = u_cod
          AND usado = FALSE
          AND ativo = TRUE
          AND dataValidade >= NOW()
        LIMIT 1;

        IF codCupom IS NOT NULL THEN
            SET vT = vT - (vT * (descontoCupom / 100));
            UPDATE Cupom SET usado = TRUE WHERE codCupom = codCupom;
        ELSE
            SELECT 'Aviso: Cupom inválido, expirado, já usado ou não pertence ao usuário.' AS Aviso;
        END IF;
    END IF;

    -- Soma o frete se for entrega
    IF v_tipoRetirada = 'Entrega' THEN
        SET vT = vT + v_frete;
    END IF;

    -- Verifica se já existe comprovante
    SELECT codComp INTO comP
    FROM Comprovante
    WHERE codVenda = codV AND codUsuario = u_cod
    LIMIT 1;

    IF comP IS NOT NULL THEN
        SELECT 'Erro: Comprovante já existe para esta venda e usuário.' AS Erro;
       SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Erro, comprovante já existe';
    END IF;

    -- Cria o comprovante
    INSERT INTO Comprovante (codVenda, valorTotal, codUsuario)
    VALUES (codV, vT, u_cod);

    -- Atualiza a venda e finaliza
    UPDATE Venda
    SET valorTotalVenda = vT,
        situacao = 'Finalizada'
    WHERE codVenda = codV AND codUsuario = u_cod;

	-- Se for entrega, cria registro em Entrega e Entrega_Produto
    IF v_tipoRetirada = 'Entrega' THEN
        INSERT INTO Entrega (
            codVenda,
            codUsuario,
            dataInicial,
            Situacao,
            valorTotal,
            retirada
        )
        VALUES (
            codV,
            u_cod,
            CURDATE(),
            'Em andamento',
            vT,
            'Entrega'
        );

        SET codEn = LAST_INSERT_ID();

        INSERT INTO Entrega_Produto (
            codProd,
            nomeProduto,
            Valor,
            Quantidade,
            Imagem,
            codEntrega
        )
        SELECT 
            ic.codProd,
            p.nomeProduto,
            ic.valorProduto,
            ic.quantidade,
            p.Imagens,
            codEn
        FROM ItemCarrinho ic
        JOIN Produto p ON ic.codProd = p.codProd
        WHERE ic.codCarrinho = codCarr
        ON DUPLICATE KEY UPDATE
            Quantidade = VALUES(Quantidade),
            Valor = VALUES(Valor),
            Imagem = VALUES(Imagem);

        INSERT INTO log_debug(mensagem, dataLog)
        VALUES (CONCAT('Entrega criada e produtos vinculados para venda ', codV), NOW());
    ELSE
     INSERT INTO Entrega (
            codVenda,
            codUsuario,
            dataInicial,
            Situacao,
            valorTotal,
            retirada
        )
        VALUES (
            codV,
            u_cod,
            CURDATE(),
            'Em andamento',
            vT,
            'Local'
        );

        SET codEn = LAST_INSERT_ID();

        INSERT INTO Entrega_Produto (
            codProd,
            nomeProduto,
            Valor,
            Quantidade,
            Imagem,
            codEntrega
        )
        SELECT 
            ic.codProd,
            p.nomeProduto,
            ic.valorProduto,
            ic.quantidade,
            p.Imagens,
            codEn
        FROM ItemCarrinho ic
        JOIN Produto p ON ic.codProd = p.codProd
        WHERE ic.codCarrinho = codCarr
        ON DUPLICATE KEY UPDATE
            Quantidade = VALUES(Quantidade),
            Valor = VALUES(Valor),
            Imagem = VALUES(Imagem);
    
        -- Retirada local: apenas registra log
        INSERT INTO log_debug(mensagem, dataLog)
        VALUES (CONCAT('Venda ', codV, ' finalizada com retirada local.'), NOW());
    END IF;

    -- Mensagem final
    SELECT 
        'Compra finalizada com sucesso!' AS Sucesso,
        vT AS ValorFinalComFrete,
        v_tipoRetirada AS TipoRetirada,
        codV AS CodigoVenda;

END $$
DELIMITER ;

-- call concluir_compra(1,null,0,'Local');

select * from Carrinho;
select * from ItemCarrinho;
select * from Venda;
select * from Entrega;

Delimiter $$
drop procedure if exists deletar_fornecedor $$
create procedure deletar_fornecedor(f_cod int)
begin 

	Delete from Fornecedor where codF = f_cod;

end $$


select * from Venda;
select * from ItemCarrinho;
 -- call concluir_compra(2,1, null,30);

select * from Venda;
select * from Log_debug;
select * from Comprovante;

-- VERSÃO 4!
Delimiter $$
DROP TRIGGER IF EXISTS trigger_finaliza_venda $$
CREATE TRIGGER trigger_finaliza_venda
AFTER UPDATE ON Venda
FOR EACH ROW
BEGIN
    DECLARE tipoRetirada ENUM('Local','Entrega');
    DECLARE existeEntrega INT DEFAULT 0;

    -- Só atua se a venda mudou de situação e foi finalizada
    IF NEW.situacao = 'Finalizada' AND OLD.situacao <> 'Finalizada' THEN
        
        -- Verifica se já existe uma entrega associada
        SELECT COUNT(*) INTO existeEntrega
        FROM Entrega
        WHERE codVenda = NEW.codVenda;

        -- Se houver entrega registrada, pegamos o tipo
        IF existeEntrega > 0 THEN
            SELECT retirada INTO tipoRetirada
            FROM Entrega
            WHERE codVenda = NEW.codVenda
            LIMIT 1;
        ELSE
            SET tipoRetirada = 'Local';
        END IF;

        -- Logs conforme o tipo
        IF tipoRetirada = 'Entrega' THEN
            INSERT INTO log_debug (mensagem, dataLog)
            VALUES (
                CONCAT('Trigger: venda ', NEW.codVenda, 
                       ' finalizada com ENTREGA registrada. Valor total: ', NEW.valorTotalVenda),
                NOW()
            );
        ELSE
            INSERT INTO log_debug (mensagem, dataLog)
            VALUES (
                CONCAT('Trigger: venda ', NEW.codVenda, 
                       ' finalizada com RETIRADA LOCAL. Valor total: ', NEW.valorTotalVenda),
                NOW()
            );
        END IF;

    END IF;
END $$

DELIMITER ;

describe Produto;
describe Entrega_Produto;
select * from Entrega;
select * from Venda;

DELIMITER $$
CREATE PROCEDURE deletar_avaliacao (
    IN p_codProd INT,
    IN p_codUsuario INT
)
BEGIN
    -- Verifica se a avaliação existe
    IF EXISTS (
        SELECT 1 FROM Avaliacao WHERE codProd = p_codProd AND codUsuario = p_codUsuario
    ) THEN
        DELETE FROM Avaliacao WHERE codProd = p_codProd AND codUsuario = p_codUsuario;
        SELECT 'Avaliação deletada com sucesso!' AS Sucesso;
    ELSE
        SELECT 'Avaliação não encontrada.' AS Erro;
    END IF;
END $$

call deletar_avaliacao(1,2);


Delimiter $$
CREATE PROCEDURE deletar_favorito (
    IN p_codProd INT,
    IN p_codUsuario INT
)
BEGIN
    -- Verifica se o favorito existe antes de deletar
    IF EXISTS (
        SELECT 1 FROM wishlist WHERE codProd = p_codProd AND codUsuario = p_codUsuario
    ) THEN
        DELETE FROM wishlist WHERE codProd = p_codProd AND codUsuario = p_codUsuario;
        SELECT 'Produto removido dos favoritos.' AS Sucesso;
    ELSE
        SELECT 'Produto não encontrado nos favoritos.' AS Info;
    END IF;
END $$

call deletar_favorito(1,2);




-- FUNÇÕES DO SISTEMA

-- login
Delimiter $$
Create procedure buscar_usuario (p_email varchar(150))
begin

			select codUsuario, role, Nome, Email, Senha, Ativo
            from Usuario
            where Email = p_email;
End $$


-- ATIVAR O CLIENTE DESATIVADO

select * from Usuario;
DELIMITER $$
DROP PROCEDURE IF EXISTS ativar_cliente $$
CREATE PROCEDURE ativar_cliente(c_email VARCHAR(150))
BEGIN
    DECLARE v_codUsuario INT;

    -- Verifica se o usuário com o e-mail existe e está desativado
    SELECT codUsuario INTO v_codUsuario
    FROM Usuario
    WHERE Email = c_email AND Ativo = '0'
    LIMIT 1;

    IF v_codUsuario IS NOT NULL THEN

        -- Reativa o usuário
        UPDATE Usuario
        SET Ativo = '1'
        WHERE codUsuario = v_codUsuario;

        -- Reativa vendas canceladas (por desativação)
        UPDATE Venda
        SET situacao = 'Em andamento'
        WHERE codUsuario = v_codUsuario AND situacao = 'Cancelada';

        SELECT 'Cliente ativado com sucesso.' AS Sucesso;

    ELSE
        SELECT 'Usuário já está ativo ou não existe.' AS Info;
    END IF;
END $$

call ativar_cliente(1);



-- -------------------- --
-- 		EVENTO  		--
-- -------------------- --

SET GLOBAL event_scheduler = ON;

DELIMITER $
CREATE EVENT IF NOT EXISTS gerar_cupons_periodico
ON SCHEDULE EVERY 1 DAY -- roda todo dia, pode ajustar o intervalo
DO
BEGIN
    DECLARE v_codUsuario INT;
    DECLARE v_totalCompras INT;
    DECLARE v_cupomCodigo VARCHAR(20);
    DECLARE v_desconto DOUBLE DEFAULT 10; -- exemplo de desconto

    -- Cursor para percorrer todos os usuários que fizeram compras
    DECLARE curUsuarios CURSOR FOR 
        SELECT codUsuario FROM Usuario;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_codUsuario = NULL;

    OPEN curUsuarios;

    read_loop: LOOP
        FETCH curUsuarios INTO v_codUsuario;
        IF v_codUsuario IS NULL THEN
            LEAVE read_loop;
        END IF;

        -- Conta compras finalizadas do usuário
        SET v_totalCompras = (
            SELECT COUNT(*) 
            FROM Venda v
            JOIN Cliente c ON v.codCli = c.codCli
            WHERE c.codUsuario = v_codUsuario AND v.situacao = 'Finalizada'
        );

        -- Para cada 10 compras finalizadas, gera um cupom novo
        IF v_totalCompras > 0 AND v_totalCompras % 10 = 0 THEN
            -- Gera um código de cupom simples aleatório
            SET v_cupomCodigo = CONCAT('CUPOM', LPAD(FLOOR(RAND() * 99999), 5, '0'));

            -- Insere o cupom para o usuário
            INSERT INTO Cupom (codUsuario, codigo, desconto, dataCriacao, dataValidade, usado, ativo)
            VALUES (
                v_codUsuario,
                v_cupomCodigo,
                v_desconto,
                NOW(),
                DATE_ADD(NOW(), INTERVAL 30 DAY), -- validade de 30 dias
                FALSE,
                TRUE
            );
        END IF;
    END LOOP;

    CLOSE curUsuarios;

END $$



Delimiter $$
DROP EVENT IF EXISTS Verificar_zeta_pass	$$
CREATE EVENT Verificar_zeta_pass
ON SCHEDULE EVERY 1 day
DO
BEGIN
	
	  IF EXISTS (SELECT 1 FROM ZetaPass WHERE situacao = '1') THEN
    -- Inserir log para zeta que vence hoje
    INSERT INTO log_debug (mensagem, dataLog)
    SELECT 
        CONCAT(' ZetaPass do usuário ', U.Nome, ' (codUsuario=', Z.codUsuario, ') vence hoje.')
        , NOW()
    FROM ZetaPass Z
    JOIN Usuario U ON U.codUsuario = Z.codUsuario
    WHERE Z.dataFinal = CURDATE()
      AND Z.situacao = '1';

    -- Atualizar situação para 0 (inativo) para expirados
    UPDATE ZetaPass Z
    SET Z.situacao = '0'
    WHERE Z.dataFinal < CURDATE()
      AND Z.situacao = '1';

    -- Inserir log para zeta expirado e desativado
    INSERT INTO log_debug (mensagem, dataLog)
    SELECT 
        CONCAT(' ZetaPass expirado e desativado: usuário ', U.Nome, ' (codUsuario=', Z.codUsuario, ') - expirou em ', DATE_FORMAT(Z.dataFinal, '%Y-%m-%d'))
        , NOW()
    FROM ZetaPass Z
    JOIN Usuario U ON U.codUsuario = Z.codUsuario
    WHERE Z.dataFinal < CURDATE()
      AND Z.situacao = '0';
	end if;
END$$
DELIMITER ;



-- ======================
--  LISTAGENS       =====
-- ======================

-- Listar e obter Produto

Delimiter $$
drop procedure if exists sp_listar_produtos $$
CREATE PROCEDURE sp_listar_produtos()
BEGIN
    SELECT p.codProd, p.nomeProduto, p.Descricao, p.Valor, p.Quantidade, c.nomeCategoria, f.Nome AS Fornecedor
    FROM Produto p
    LEFT JOIN Categorias c ON p.codCat = c.codCat
    LEFT JOIN Fornecedor f ON p.codF = f.codF;
END $$

Delimiter $$
drop procedure if exists sp_obter_produto_por_id $$
CREATE  PROCEDURE sp_obter_produto_por_id(IN p_id INT)
BEGIN
    SELECT codProd, nomeProduto, Descricao, Valor
    FROM Produto
    WHERE codProd = p_id;
END $$


-- Listar e obter categorias

Delimiter $$
drop procedure if exists sp_listar_categorias $$
CREATE  PROCEDURE sp_listar_categorias()
BEGIN
    SELECT codCat, nomeCategoria
    FROM Categorias
    ORDER BY nomeCategoria;
END $$

Delimiter $$
drop procedure if exists sp_obter_categoria $$
CREATE  PROCEDURE sp_obter_categoria(IN p_id INT)
BEGIN
    SELECT codCat, nomeCategoria
    FROM Categorias
    WHERE codCat = p_id;
END $$



-- Listar Usuario e Obter
Delimiter $$
drop  procedure if exists sp_obter_cliente $$
CREATE PROCEDURE sp_obter_cliente(IN p_id INT)
BEGIN
    SELECT codUsuario, Nome, Email
    FROM Usuario
    WHERE codUsuario = p_id AND Role = 'Cliente';
END $$


Delimiter $$
drop procedure if exists sp_usuario_buscar_por_id $$
CREATE  PROCEDURE sp_usuario_buscar_por_id(IN p_id INT)
BEGIN
    SELECT codUsuario, Role, Nome, Email, Ativo, Foto, Telefone
    FROM Usuario
    WHERE codUsuario = p_id;
END $$


Delimiter $$
drop procedure if exists sp_usuario_listar_ativos $$
CREATE  PROCEDURE sp_usuario_listar_ativos()
BEGIN
    SELECT codUsuario, Nome, Email, Role, Ativo
    FROM Usuario
    WHERE Ativo = '1';
END $$

Delimiter $$
drop procedure if exists sp_usuario_listar_inativos $$
CREATE  PROCEDURE sp_usuario_listar_inativos()
BEGIN
    SELECT codUsuario, Nome, Email, Role, Ativo
    FROM Usuario
    WHERE Ativo = '0';
END $$
 
 
 DELIMITER $$
DROP PROCEDURE IF EXISTS sp_usuario_atualizar_status $$
CREATE PROCEDURE sp_usuario_atualizar_status (
    IN p_id INT,
    IN p_status CHAR(1)
)
BEGIN
    UPDATE Usuario
    SET Ativo = p_status
    WHERE codUsuario = p_id;
END $$
DELIMITER ;

Delimiter $$
drop procedure if exists sp_listar_vendas $$
create procedure sp_listar_vendas(in n_u varchar(100), in s_t varchar(100))
begin

    Select Distinct
    v.codVenda,
    v.codUsuario,
    v.situacao,
    v.dataE,
    u.Nome
    From Venda v
    inner join Usuario u on v.codUsuario = u.codUsuario
    where
    ((n_u is not null and n_u <>'' and u.Nome like concat('%', n_u, '%'))
        or
     (s_t is not null and s_t <>'' and v.situacao like concat('%',s_t,'%'))
        or 
     ((n_u is null or s_t = '') and (s_t is null or n_u=''))
    )
    order by u.Nome;

end $$
Delimiter ;
