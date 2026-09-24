"""
Popula o DistributedOrderSystem com dados de teste realistas via chamadas HTTP
reais aos serviços (GatewayBff + OrderService + UserService) — sem inserir
direto no banco, então passa pelas mesmas regras de negócio que o app real
usaria (validação de payload, máquina de estados de OrderStatus, política de
senha do Identity, evento Kafka `order-created`).

Cobre: Product + Inventory (via GatewayBff), Order (via GatewayBff +
OrderService), Customer (via UserService/AuthController.Register).

IMPORTANTE — limite conhecido do domínio: o agregado `Order` (OrderService)
não tem campo CustomerId (ver Domain/Aggregates/Order.cs). Os clientes criados
aqui NÃO ficam vinculados aos pedidos criados aqui — são dados independentes,
cada um populando seu próprio serviço. Se precisar da referência
pedido->cliente, isso exige alterar o domínio do OrderService (fora do escopo
deste script).

Pré-requisitos (serviços já de pé antes de rodar):
  - docker compose -f docker/docker-compose.yml up -d dos_postgres dos_redis dos_kafka dos_kafka_ui
  - OrderService     em http://localhost:5003
  - ProductService   em http://localhost:5198  (chamado indiretamente via GatewayBff)
  - InventoryService em http://localhost:5051  (idem)
  - GatewayBff       em http://localhost:5189
  - UserService       em http://localhost:5010  (não passa pelo Gateway)

Uso:
  E:/conda/envs/ai-engineer-course/python.exe scripts/seed_mock_data.py

Idempotência: NÃO é idempotente — cada execução cria produtos, pedidos e
clientes novos (emails de cliente levam um sufixo aleatório por execução,
pra não colidir com "email já registrado"). Rodar de novo só acumula mais
dados.
"""

import random
import sys
import uuid

import httpx

GATEWAY_URL = "http://localhost:5189"
ORDER_SERVICE_URL = "http://localhost:5003"  # troca de status não é exposta pelo Gateway
USER_SERVICE_URL = "http://localhost:5010"  # cadastro de cliente não passa pelo Gateway

PRODUCTS = [
    {"name": 'Notebook Ultra 15"', "price": 4899.90, "description": "Notebook 15 polegadas, 16GB RAM, SSD 512GB"},
    {"name": "Mouse Sem Fio Pro", "price": 129.90, "description": "Mouse ergonômico sem fio, 2.4GHz"},
    {"name": "Teclado Mecânico RGB", "price": 349.00, "description": "Teclado mecânico switch blue, iluminação RGB"},
    {"name": 'Monitor 27" 4K', "price": 2199.00, "description": "Monitor IPS 27 polegadas, resolução 4K"},
    {"name": "Fone Bluetooth ANC", "price": 599.90, "description": "Fone over-ear com cancelamento de ruído"},
    {"name": "Webcam Full HD", "price": 249.90, "description": "Webcam 1080p com microfone integrado"},
    {"name": "SSD NVMe 1TB", "price": 459.00, "description": "SSD NVMe PCIe Gen4, 1TB"},
    {"name": "Carregador USB-C 65W", "price": 149.90, "description": "Carregador rápido GaN, 65W"},
    {"name": "Cadeira Ergonômica", "price": 1299.00, "description": "Cadeira de escritório com apoio lombar"},
    {"name": "Hub USB-C 7 em 1", "price": 199.90, "description": "Hub USB-C com HDMI, USB 3.0 e leitor de cartão"},
    {"name": "Impressora Multifuncional", "price": 899.00, "description": "Impressora jato de tinta com scanner e Wi-Fi"},
    {"name": "Roteador Wi-Fi 6", "price": 549.90, "description": "Roteador dual-band Wi-Fi 6, até 1800 Mbps"},
    {"name": "Power Bank 20000mAh", "price": 179.90, "description": "Bateria portátil com carga rápida USB-C"},
    {"name": "Mousepad Gamer XL", "price": 79.90, "description": "Mousepad grande com base emborrachada"},
    {"name": "Caixa de Som Bluetooth", "price": 329.00, "description": "Caixa de som portátil à prova d'água"},
    {"name": "Smartwatch Fitness", "price": 799.00, "description": "Relógio inteligente com monitor cardíaco"},
    {"name": "Tablet 10 polegadas", "price": 1599.00, "description": "Tablet Android 10\", 128GB, Wi-Fi"},
    {"name": "Pen Drive 128GB", "price": 89.90, "description": "Pen drive USB 3.0, leitura rápida"},
    {"name": "HD Externo 2TB", "price": 469.90, "description": "HD externo portátil USB 3.0, 2TB"},
    {"name": "Placa de Vídeo RTX", "price": 3299.00, "description": "Placa de vídeo gamer, 12GB VRAM"},
    {"name": "Memória RAM 16GB DDR5", "price": 429.00, "description": "Módulo de memória DDR5 16GB 5600MHz"},
    {"name": "Fonte ATX 650W", "price": 389.90, "description": "Fonte de alimentação 80 Plus Bronze, 650W"},
    {"name": "Gabinete Gamer RGB", "price": 349.00, "description": "Gabinete mid-tower com iluminação RGB"},
    {"name": "Cooler para CPU", "price": 159.90, "description": "Cooler air a ar com dissipador de cobre"},
    {"name": "Cabo HDMI 2.1", "price": 49.90, "description": "Cabo HDMI 2.1, 2 metros, suporte a 4K 120Hz"},
    {"name": "Adaptador USB-C para HDMI", "price": 99.90, "description": "Adaptador multiportas USB-C para HDMI"},
    {"name": "Suporte para Notebook", "price": 139.90, "description": "Suporte ergonômico ajustável em alumínio"},
    {"name": "Luminária de Mesa USB", "price": 89.90, "description": "Luminária LED com regulagem de intensidade"},
    {"name": "Organizador de Cabos", "price": 39.90, "description": "Kit organizador de cabos com velcro"},
    {"name": "Microfone USB Condensador", "price": 379.00, "description": "Microfone USB para streaming e podcast"},
]

CUSTOMERS = [
    {"first": "Maria", "last": "Silva", "local": "maria.silva"},
    {"first": "João", "last": "Santos", "local": "joao.santos"},
    {"first": "Ana", "last": "Oliveira", "local": "ana.oliveira"},
    {"first": "Pedro", "last": "Souza", "local": "pedro.souza"},
    {"first": "Juliana", "last": "Costa", "local": "juliana.costa"},
    {"first": "Lucas", "last": "Pereira", "local": "lucas.pereira"},
    {"first": "Fernanda", "last": "Almeida", "local": "fernanda.almeida"},
    {"first": "Rafael", "last": "Lima", "local": "rafael.lima"},
    {"first": "Camila", "last": "Rodrigues", "local": "camila.rodrigues"},
    {"first": "Bruno", "last": "Martins", "local": "bruno.martins"},
    {"first": "Patrícia", "last": "Carvalho", "local": "patricia.carvalho"},
    {"first": "Diego", "last": "Ferreira", "local": "diego.ferreira"},
    {"first": "Larissa", "last": "Gomes", "local": "larissa.gomes"},
    {"first": "Thiago", "last": "Barbosa", "local": "thiago.barbosa"},
    {"first": "Beatriz", "last": "Ribeiro", "local": "beatriz.ribeiro"},
]

# Caminho de transição por pedido, respeitando a máquina de estados de OrderStatus.cs:
#   Pending -> Confirmed -> Shipped -> Delivered
#   Pending -> Cancelled | Confirmed -> Cancelled
# Lista vazia = pedido fica em Pending (estado inicial, sem PUT extra).
STATUS_PLAN = (
    [[]] * 5
    + [["Confirmed"]] * 5
    + [["Cancelled"]] * 2
    + [["Confirmed", "Cancelled"]] * 2
    + [["Confirmed", "Shipped"]] * 6
    + [["Confirmed", "Shipped", "Delivered"]] * 10
)


def _request(client: httpx.Client, method: str, url: str, **kwargs) -> httpx.Response:
    resp = client.request(method, url, **kwargs)
    if resp.status_code >= 400:
        print(f"[ERRO] {method} {url} -> {resp.status_code}: {resp.text}")
        resp.raise_for_status()
    return resp


def check_service(client: httpx.Client, url: str, name: str) -> None:
    try:
        client.get(url, timeout=3.0)
    except httpx.ConnectError:
        print(f"[ERRO] {name} não está respondendo em {url}. Suba o serviço antes de rodar o seed.")
        sys.exit(1)


def create_products(client: httpx.Client) -> list[dict]:
    created = []
    for p in PRODUCTS:
        resp = _request(client, "POST", f"{GATEWAY_URL}/api/commands/products", json=p)
        product = resp.json()
        stock = random.randint(5, 80)
        _request(
            client,
            "POST",
            f"{GATEWAY_URL}/api/commands/inventory/set",
            json={"productId": product["id"], "quantity": stock},
        )
        created.append(product)
        print(f"  Produto criado: {product['name']} (R$ {product['price']:.2f}) - estoque {stock}")
    return created


def create_order(client: httpx.Client, products: list[dict]) -> dict:
    chosen = random.sample(products, random.randint(1, 4))
    items = [
        {"productId": p["id"], "quantity": random.randint(1, 3), "unitPrice": p["price"]}
        for p in chosen
    ]
    resp = _request(client, "POST", f"{GATEWAY_URL}/api/commands/orders", json={"items": items})
    return resp.json()


def transition_order(client: httpx.Client, order_id: int, path: list[str]) -> None:
    for status in path:
        _request(
            client,
            "PUT",
            f"{ORDER_SERVICE_URL}/api/commands/orders/{order_id}/status",
            json={"status": status},
        )


def create_customers(client: httpx.Client, run_tag: str) -> list[dict]:
    created = []
    for c in CUSTOMERS:
        email = f"{c['local']}.{run_tag}@example.com"
        payload = {
            "email": email,
            "password": "MockUser123",
            "firstName": c["first"],
            "lastName": c["last"],
            "phoneNumber": f"+55 11 9{random.randint(1000, 9999)}-{random.randint(1000, 9999)}",
        }
        resp = _request(client, "POST", f"{USER_SERVICE_URL}/api/auth/register", json=payload)
        customer = resp.json()
        created.append(customer)
        print(f"  Cliente criado: {c['first']} {c['last']} ({email})")
    return created


def main() -> None:
    run_tag = uuid.uuid4().hex[:6]
    with httpx.Client(timeout=10.0) as client:
        print("Verificando serviços...")
        check_service(client, f"{GATEWAY_URL}/api/queries/orders", "GatewayBff")
        check_service(client, f"{ORDER_SERVICE_URL}/api/orders", "OrderService")
        check_service(
            client,
            f"{USER_SERVICE_URL}/api/auth/profile/00000000-0000-0000-0000-000000000000",
            "UserService",
        )

        print("\nCriando produtos + estoque...")
        products = create_products(client)

        print("\nCriando pedidos...")
        results = []
        for path in STATUS_PLAN:
            order = create_order(client, products)
            transition_order(client, order["orderId"], path)
            final_status = path[-1] if path else "Pending"
            results.append((order["orderId"], final_status, order["total"]))
            print(f"  Pedido #{order['orderId']}: {final_status} - R$ {order['total']:.2f}")

        print("\nCriando clientes...")
        customers = create_customers(client, run_tag)

        print("\n=== Resumo de pedidos ===")
        for order_id, status, total in results:
            print(f"  #{order_id:<4} {status:<10} R$ {total:.2f}")

        print(
            f"\n{len(products)} produtos, {len(results)} pedidos e {len(customers)} "
            "clientes criados."
        )
        print("Use um dos IDs de pedido acima em POST /api/chat/message do ChatbotService.")
        print(
            "Nota: Order não tem CustomerId no domínio atual (OrderService/Domain/Aggregates/Order.cs) "
            "- pedidos e clientes criados aqui não estão vinculados entre si."
        )


if __name__ == "__main__":
    main()
