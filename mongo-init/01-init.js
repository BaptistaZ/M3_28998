// ===============================================================
// Script de inicialização do MongoDB
// Criado automaticamente no arranque do container (docker-init)
// Objetivo: Criar utilizador de aplicação com acesso readWrite
// ===============================================================

const databaseName = "integracao_28998";
const username     = "tiago_28998";
const password     = "28998";

// Seleciona (ou cria) a base de dados alvo
const db = db.getSiblingDB(databaseName);

// Verifica se o utilizador já existe
const userExists = db.getUser(username);

if (userExists) {
    print(`>> Utilizador '${username}' já existe na base '${databaseName}'. Nenhuma ação necessária.`);
} else {
    db.createUser({
        user: username,
        pwd: password,
        roles: [
            { role: "readWrite", db: databaseName }
        ]
    });

    print(`>> Utilizador '${username}' criado com sucesso com permissões readWrite na base '${databaseName}'.`);
}