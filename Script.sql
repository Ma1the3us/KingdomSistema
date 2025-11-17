-- =====================================================
-- Banco de Dados: MeuProjetoMVC
-- =====================================================
drop database if exists meuprojetomvc;
CREATE DATABASE IF NOT EXISTS MeuProjetoMVC
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_general_ci;

USE MeuProjetoMVC;

-- =====================================================
-- 1. Tabela Usuario
-- =====================================================
CREATE TABLE Usuario (
    codUsuario INT AUTO_INCREMENT PRIMARY KEY,
    Nome       VARCHAR(100) NOT NULL,
    Email      VARCHAR(150) NOT NULL UNIQUE,
    Senha      VARCHAR(255) NOT NULL,
    Role       ENUM('Admin','Cliente') NOT NULL DEFAULT 'Cliente'
    
);

ALTER TABLE Usuario
    ADD COLUMN Ativo CHAR(1) NOT NULL DEFAULT 'S';
    ALTER TABLE Usuario
    MODIFY Role ENUM('Admin','Cliente','Funcionario');

-- =====================================================
-- 2. Tabela Fornecedor
-- =====================================================
CREATE TABLE Fornecedor (
    codF INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    CNPJ BIGINT NOT NULL,
    Nome VARCHAR(100) NOT NULL,
    UNIQUE KEY uq_fornecedor_cnpj (CNPJ)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 3. Tabela Produto
-- =====================================================
CREATE TABLE Produto (
    CodProd INT AUTO_INCREMENT PRIMARY KEY,
    nomeProduto VARCHAR(150) NOT NULL,
    Descricao TEXT NULL,
    Valor DECIMAL(10,2) NOT NULL,
    Imagens LONGBLOB NULL
);

ALTER TABLE Produto ADD COLUMN Quantidade INT NOT NULL DEFAULT 1;
ALTER TABLE Produto ADD COLUMN quantidadeTotal INT NOT NULL DEFAULT 1;
ALTER TABLE Produto ADD COLUMN codCat INT DEFAULT NULL;
ALTER TABLE Produto ADD COLUMN codF INT DEFAULT NULL;
ALTER TABLE Produto MODIFY COLUMN Descricao VARCHAR(255) NOT NULL DEFAULT '';
ALTER TABLE Produto
    ADD CONSTRAINT fk_produto_fornecedor
    FOREIGN KEY (codF) REFERENCES Fornecedor(codF)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

-- =====================================================
-- 4. Tabela Pedidos
-- =====================================================
CREATE TABLE Pedidos (
    PedidoId INT AUTO_INCREMENT PRIMARY KEY,
    NomeCliente VARCHAR(150) NOT NULL,
    EmailCliente VARCHAR(150) NOT NULL,
    Total DECIMAL(10,2) NOT NULL,
    FormaPagamento VARCHAR(50) NOT NULL,
    DataPedido DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- 5. Tabela ItensPedido
-- =====================================================
CREATE TABLE ItensPedido (
    ItemId INT AUTO_INCREMENT PRIMARY KEY,
    PedidoId INT NOT NULL,
    ProdutoId INT NOT NULL,
    Quantidade INT NOT NULL,
    ValorUnitario DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (PedidoId) REFERENCES Pedidos(PedidoId) ON DELETE CASCADE,
    FOREIGN KEY (ProdutoId) REFERENCES Produto(codProd) ON DELETE CASCADE
);

-- =====================================================
-- 6. Tabela Categorias
-- =====================================================
CREATE TABLE Categorias (
    codCat INT PRIMARY KEY AUTO_INCREMENT,
    nomeCategoria VARCHAR(100)
);

ALTER TABLE Produto
    ADD FOREIGN KEY (codCat) REFERENCES Categorias(codCat);

-- =====================================================
-- 7. Tabela Vendas
-- =====================================================
CREATE TABLE Vendas (
    codVenda INT AUTO_INCREMENT PRIMARY KEY,
    codUsuario INT NOT NULL,
    valorTotal DECIMAL(10,2) NOT NULL,
    formaPagamento VARCHAR(50) NOT NULL,
    dataVenda DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (codUsuario) REFERENCES Usuario(codUsuario)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- =====================================================
-- 8. Tabela ItensVenda
-- =====================================================
CREATE TABLE ItensVenda (
    codItemVenda INT AUTO_INCREMENT PRIMARY KEY,
    codVenda INT NOT NULL,
    codProduto INT NOT NULL,
    quantidade INT NOT NULL,
    valorUnitario DECIMAL(10,2) NOT NULL,
    subtotal DECIMAL(10,2) GENERATED ALWAYS AS (quantidade * valorUnitario) STORED,
    FOREIGN KEY (codVenda) REFERENCES Vendas(codVenda) ON DELETE CASCADE,
    FOREIGN KEY (codProduto) REFERENCES Produto(codProd) ON DELETE CASCADE
);

-- =====================================================
-- Usuários de Teste
-- =====================================================
/*INSERT INTO Usuario (Nome, Email, Senha, Role) VALUES
('Administrador', 'admin@meuprojeto.com',
 '$2a$12$ExID6a2jgxZ5CfdgkbtiUu7bm51jAzG7xqYw.3C3Z/qZ6jNobY7sG', 'Admin'),
('Cliente Padrão', 'cliente@meuprojeto.com',
 '$2a$12$ExID6a2jgxZ5CfdgkbtiUu7bm51jAzG7xqYw.3C3Z/qZ6jNobY7sG', 'Cliente');*/

INSERT INTO Produto (NomeProduto, Descricao, Valor)
VALUES
('Camiseta MVC', 'Camiseta personalizada MeuProjetoMVC', 59.90),
('Caneca MVC', 'Caneca oficial do projeto', 29.90);

/*INSERT INTO Usuario (Nome, Email, Senha, Role) VALUES
('Administrador', 'admin@meuprojeto1.com', '123', 'Admin');*/

-- =====================================================
-- Procedures de Usuário
-- =====================================================
DELIMITER $$

DROP PROCEDURE IF EXISTS sp_usuario_criar $$
CREATE PROCEDURE sp_usuario_criar (
    IN p_role ENUM('Funcionario','Admin','Cliente'),
    IN p_nome VARCHAR(100),
    IN p_email VARCHAR(150),
    IN p_senha VARCHAR(100),
    IN p_ativo CHAR(1)
)
BEGIN
    IF NOT EXISTS (SELECT codUsuario FROM Usuario WHERE Email = p_email) THEN
        INSERT INTO Usuario(Role, Nome, Email, Senha, Ativo)
        VALUES (p_role, p_nome, p_email, p_senha, p_ativo);
    ELSE
        SELECT 'Erro: Usuário já existe.' AS Mensagem;
    END IF;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_usuario_listar_ativos$$
CREATE PROCEDURE sp_usuario_listar_ativos()
BEGIN
    SELECT codUsuario, Nome, Email, Role, Ativo
    FROM Usuario
    WHERE Ativo = 'S';
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_usuario_listar_inativos $$
CREATE PROCEDURE sp_usuario_listar_inativos()
BEGIN
    SELECT codUsuario, Nome, Email, Role, Ativo
    FROM Usuario
    WHERE Ativo = 'N';
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_usuario_atualizar $$
CREATE PROCEDURE sp_usuario_atualizar(
    IN p_codUsuario INT,
    IN p_role ENUM('Funcionario','Admin','Cliente'),
    IN p_nome VARCHAR(100),
    IN p_email VARCHAR(150),
    IN p_senha VARCHAR(100),
    IN p_ativo CHAR(1)
)
BEGIN
    UPDATE Usuario
       SET Role  = p_role,
           Nome  = p_nome,
           Email = p_email,
           Senha = p_senha,
           Ativo = p_ativo
     WHERE codUsuario = p_codUsuario;
END $$
DELIMITER ;

-- =====================================================
-- Procedures de Produto
-- =====================================================
DELIMITER $$
DROP PROCEDURE IF EXISTS cad_Produto $$
CREATE PROCEDURE cad_Produto(
    IN p_quantidade INT,
    IN p_imagens LONGBLOB,
    IN p_valor DOUBLE,
    IN p_descricao VARCHAR(255),
    IN p_nomeproduto VARCHAR(100),
    IN p_codCat INT,
    IN p_codF INT
)
BEGIN
    IF NOT EXISTS (SELECT codProd FROM Produto WHERE nomeProduto = p_nomeproduto) THEN
        INSERT INTO Produto
            (Quantidade, Imagens, Valor, Descricao, nomeProduto, codCat, codF)
        VALUES
            (p_quantidade, p_imagens, p_valor, p_descricao, p_nomeproduto, p_codCat, p_codF);
    END IF;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS editar_produto $$
CREATE PROCEDURE editar_produto(
    IN p_cod INT,
    IN p_quant INT,
    IN p_valor DOUBLE,
    IN p_nome VARCHAR(100),
    IN p_descricao VARCHAR(255),
    IN p_imagens LONGBLOB
)
BEGIN
    UPDATE Produto
       SET Quantidade = p_quant,
           Valor = p_valor,
           nomeProduto = p_nome,
           Descricao = p_descricao,
           Imagens = p_imagens
     WHERE codProd = p_cod;
END $$
DELIMITER ;

-- =====================================================
-- Procedures de Fornecedor
-- =====================================================
DELIMITER $$
DROP PROCEDURE IF EXISTS cad_Fornecedor $$
CREATE PROCEDURE cad_Fornecedor(
    IN p_CNPJ BIGINT,
    IN p_Nome VARCHAR(100)
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Fornecedor WHERE CNPJ = p_CNPJ) THEN
        INSERT INTO Fornecedor (CNPJ, Nome)
        VALUES (p_CNPJ, p_Nome);
    ELSE
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Já existe um fornecedor cadastrado com este CNPJ.';
    END IF;
END $$
DELIMITER ;

-- =====================================================
-- Procedures de Categoria
-- =====================================================
DELIMITER $$
DROP PROCEDURE IF EXISTS cad_Categoria $$
CREATE PROCEDURE cad_Categoria(
    IN p_Nome VARCHAR(100)
)
BEGIN
    IF NOT EXISTS (SELECT codCat FROM Categorias WHERE nomeCategoria = p_Nome) THEN
        INSERT INTO Categorias (nomeCategoria)
        VALUES (p_Nome);
    END IF;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_listar_categorias $$
CREATE PROCEDURE sp_listar_categorias()
BEGIN
    SELECT codCat, nomeCategoria
    FROM Categorias
    ORDER BY nomeCategoria;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_obter_categoria $$
CREATE PROCEDURE sp_obter_categoria(IN p_id INT)
BEGIN
    SELECT codCat, nomeCategoria
    FROM Categorias
    WHERE codCat = p_id;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_atualizar_categoria $$
CREATE PROCEDURE sp_atualizar_categoria(IN p_id INT, IN p_nome VARCHAR(100))
BEGIN
    UPDATE Categorias
    SET nomeCategoria = p_nome
    WHERE codCat = p_id;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_excluir_categoria $$
CREATE PROCEDURE sp_excluir_categoria(IN p_id INT)
BEGIN
    DELETE FROM Categorias WHERE codCat = p_id;
END $$
DELIMITER ;

-- =====================================================
-- Procedure de Vendas
-- =====================================================
DELIMITER $$
DROP PROCEDURE IF EXISTS sp_registrar_venda $$
CREATE PROCEDURE sp_registrar_venda (
    IN p_codUsuario INT,
    IN p_valorTotal DECIMAL(10,2),
    IN p_formaPagamento VARCHAR(50)
)
BEGIN
    INSERT INTO Vendas (codUsuario, valorTotal, formaPagamento)
    VALUES (p_codUsuario, p_valorTotal, p_formaPagamento);
    SELECT LAST_INSERT_ID() AS codVenda;
END $$
DELIMITER ;



DELIMITER $$
DROP PROCEDURE IF EXISTS sp_finalizar_compra $$
CREATE PROCEDURE sp_finalizar_compra (
    IN p_idCliente INT,
    IN p_formaPagamento VARCHAR(50),
    IN p_total DECIMAL(10,2)
)
BEGIN
    DECLARE v_nome VARCHAR(150);
    DECLARE v_email VARCHAR(150);
    SELECT Nome, Email INTO v_nome, v_email
    FROM Usuario
    WHERE codUsuario = p_idCliente;
    INSERT INTO Pedidos (NomeCliente, EmailCliente, Total, FormaPagamento)
    VALUES (v_nome, v_email, p_total, p_formaPagamento);
    SELECT LAST_INSERT_ID() AS NovoPedidoId;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_inserir_usuario $$
CREATE PROCEDURE sp_inserir_usuario (
    IN p_nome VARCHAR(100),
    IN p_email VARCHAR(150),
    IN p_senha VARCHAR(255),
    IN p_role ENUM('Admin','Cliente')
)
BEGIN
    INSERT INTO Usuario (Nome, Email, Senha, Role)
    VALUES (p_nome, p_email, p_senha, p_role);
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_obter_cliente $$
CREATE PROCEDURE sp_obter_cliente (IN p_id INT)
BEGIN
    SELECT codUsuario, Nome, Email
    FROM Usuario
    WHERE codUsuario = p_id AND Role = 'Cliente';
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_usuario_desativar $$
CREATE PROCEDURE sp_usuario_desativar(IN p_id INT)
BEGIN
    UPDATE Usuario SET Ativo = 'N' WHERE codUsuario = p_id;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_usuario_ativar $$
CREATE PROCEDURE sp_usuario_ativar(IN p_id INT)
BEGIN
    UPDATE Usuario SET Ativo = 'S' WHERE codUsuario = p_id;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_inserir_categoria $$
CREATE PROCEDURE sp_inserir_categoria (
    IN p_nome VARCHAR(100)
)
BEGIN
    INSERT INTO Categorias (nomeCategoria) VALUES (p_nome);
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_excluir_categoria $$
CREATE PROCEDURE sp_excluir_categoria (IN p_id INT)
BEGIN
    DELETE FROM Categorias WHERE codCat = p_id;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_inserir_produto $$
CREATE PROCEDURE sp_inserir_produto (
    IN p_nome VARCHAR(200),
    IN p_descricao VARCHAR(255),
    IN p_valor DECIMAL(10,2),
    IN p_quantidade INT,
    IN p_codCat INT,
    IN p_codF INT,
    IN p_imagens LONGBLOB
)
BEGIN
    INSERT INTO Produto (nomeProduto, Descricao, Valor, Quantidade, codCat, codF, Imagens)
    VALUES (p_nome, p_descricao, p_valor, p_quantidade, p_codCat, p_codF, p_imagens);
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_listar_produtos $$
CREATE PROCEDURE sp_listar_produtos ()
BEGIN
    SELECT p.codProd, p.nomeProduto, p.Descricao, p.Valor, p.Quantidade, c.nomeCategoria, f.Nome AS Fornecedor
    FROM Produto p
    LEFT JOIN Categorias c ON p.codCat = c.codCat
    LEFT JOIN Fornecedor f ON p.codF = f.codF;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_excluir_produto $$
CREATE PROCEDURE sp_excluir_produto (IN p_id INT)
BEGIN
    DELETE FROM Produto WHERE codProd = p_id;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS del_Categoria $$
CREATE PROCEDURE del_Categoria(IN p_id INT)
BEGIN
    DELETE FROM Categorias WHERE codCat = p_id;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS get_categoria_by_id $$
CREATE PROCEDURE get_categoria_by_id(IN p_id INT)
BEGIN
    SELECT codCat, nomeCategoria 
    FROM Categorias 
    WHERE codCat = p_id;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_obter_produto_por_id $$
CREATE PROCEDURE sp_obter_produto_por_id(IN p_id INT)
BEGIN
    SELECT codProd, nomeProduto, Descricao, Valor
    FROM Produto
    WHERE codProd = p_id;
END $$
DELIMITER ;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_usuario_buscar_por_id $$

CREATE PROCEDURE sp_usuario_buscar_por_id(IN p_id INT)
BEGIN
    SELECT codUsuario, Role, Nome, Email, Ativo
    FROM Usuario
    WHERE codUsuario = p_id;
END $$

DELIMITER ;

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
