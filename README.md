📘 M3_28998 - Arquitetura de Integração com .NET, RabbitMQ e MongoDB

Aplicação distribuída para ingestão e processamento assíncrono de dados JSON.  
Desenvolvida em **.NET 8**, com **RabbitMQ** para mensagens, **MongoDB** para persistência e **Docker Compose** para orquestração.

⸻

🚀 Funcionalidades Principais

🔹 **API REST (IngestApi)**
	•	Recebe dados JSON via endpoint `/ingest`  
	•	Publica mensagens no RabbitMQ  
	•	Documentação automática com Swagger/OpenAPI  

🔹 **Serviço Consumidor (QueueConsumer)**
	•	Lê mensagens da fila `json_ingest_28998`  
	•	Grava os dados no MongoDB local e no MongoDB Atlas  
	•	Reencaminha mensagens falhadas para a Dead Letter Queue (`.dlq`)  

🔹 **Message Broker (RabbitMQ)**
	•	Garante comunicação assíncrona e desacoplada  
	•	Interface de gestão completa (RabbitMQ Management UI)  
	•	Monitorização de filas, conexões e mensagens  

🔹 **Base de Dados (MongoDB)**
	•	Armazenamento NoSQL flexível e escalável  
	•	Suporte local e remoto (MongoDB Atlas)  
	•	Consulta e validação via MongoDB Compass  

🔹 **Orquestração (Docker Compose)**
	•	Sobe automaticamente os serviços RabbitMQ e MongoDB  
	•	Garante isolamento, volumes persistentes e redes internas  
	•	Permite levantar toda a arquitetura com um único comando  

⸻

🛠️ Tecnologias Utilizadas
	•	.NET 8 SDK  
	•	C# — ASP.NET Core & Worker Services  
	•	RabbitMQ 3.13 (management plugin)  
	•	MongoDB 7 (local e Atlas)  
	•	Docker Compose / Docker Desktop  
	•	Postman — testes de integração  

⸻

📦 Como Executar o Sistema

1️⃣ **Clonar o repositório**

git clone https://github.com/BaptistaZ/M3_28998.git  
cd M3_28998  

2️⃣ **Levantar os serviços com Docker**

docker compose down -v  
docker compose up -d  
docker ps  

📍 Serviços disponíveis:  
• RabbitMQ → http://localhost:15672 (user: tiago / pass: 12345)  
• MongoDB → mongodb://localhost:27017  

3️⃣ **Executar os projetos .NET**

dotnet run --project IngestApi --urls http://localhost:5000  
dotnet run --project QueueConsumer  

4️⃣ **Testar com Postman**

POST http://localhost:5000/ingest  
Content-Type: application/json  

{
  "aluno": 28998,
  "nome": "Tiago Baptista",
  "projeto": "Arquitetura de Integração",
  "payload": { "modulo": "EEQDS", "mensagem": "Teste final da arquitetura" }
}

✅ Resposta esperada:  
{ "messageId": "429ea4841c3a4c56954cec...29a4", "status": "queued" }  

5️⃣ **Verificar RabbitMQ**  
•	Aceder a http://localhost:15672  
•	Confirmar as filas: json_ingest_28998 e json_ingest_28998.dlq  
•	Verificar publicação e consumo das mensagens  

6️⃣ **Validar persistência no MongoDB**  
•	Aceder via terminal ou MongoDB Compass:  

mongosh  
use integracao_28998  
db.ingest.find().pretty()  

•	Confirmar o documento inserido com os campos enviados no POST  

7️⃣ **Encerrar os serviços**  

docker compose down -v  

⸻

🧩 Estrutura do Projeto

M3_28998/  
├── IngestApi/ → API REST (.NET 8)  
├── QueueConsumer/ → Serviço Worker (.NET 8)  
├── mongo-init/ → Scripts de inicialização do MongoDB  
├── docker-compose.yml → Definição dos serviços RabbitMQ e MongoDB  
└── README.md → Este documento  

⸻

🧠 Validação

✔️ A API recebe o JSON e publica no RabbitMQ  
✔️ O consumidor processa e grava no MongoDB  
✔️ A arquitetura é totalmente orquestrada via Docker Compose  
✔️ Testado ponta-a-ponta com Postman, RabbitMQ Management e MongoDB Compass  

⸻

📊 Monitorização Recomendada

	•	Docker Desktop — estado e logs dos contentores  
	•	RabbitMQ Management — filas e mensagens em tempo real  
	•	MongoDB Compass — visualização e consultas à base de dados  
	•	Postman — testes e validação do endpoint `/ingest`  

⸻

👨‍💻 Autor

**Tiago Baptista**  
📧 [tiagobaptista@ipvc.pt]  
🎓 Instituto Politécnico de Viana do Castelo — ESTG  
📘 Unidade Curricular: EEQDS — Engenharia e Qualidade de Software  

⸻

📄 Licença

Projeto académico — desenvolvido para fins de aprendizagem e demonstração técnica.  
© 2025 Tiago Baptista — Todos os direitos reservados.