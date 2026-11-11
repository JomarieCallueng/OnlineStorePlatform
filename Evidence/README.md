# Evidence

Capture the following (screenshots or a short screencast) to satisfy the rubric. Put the files in
this folder. The storefront (http://localhost:5080), the per-service Swagger pages, the RabbitMQ
management UI (http://localhost:15672), and the service logs are all you need.

1. **Successful order** — place an order and show it reach `Confirmed`, with its status history.
2. **On-demand compensation** — with the Payment failure switch ON, place an order and show it reach
   `Cancelled` with the `Compensating → Cancelled` history (stock released, refund requested).
3. **Retry recovering from an injected fault** — show the Order→Catalog typed client retrying and
   succeeding (e.g. Order logs showing a retried attempt, or stop/start Catalog briefly during a call).
4. **Correlation-ID trace across services** — show the same `X-Correlation-ID` in the Order, Inventory
   and Payment logs for one order (the id is on the response header and in each service's log scope).
5. **RabbitMQ queues incl. a `.dlq`** — the management UI showing `order.saga`, `inventory.reserve`,
   `payment.charge` and their `*.dlq` dead-letter queues.
6. **Gateway rejecting anonymous (401) and wrong-role (403)** — e.g. `POST /order/v1/orders` with no
   token (401), and an admin action (`POST /catalog/v1/products` or `PUT /inventory/v1/stock/...`) as a
   customer (403).

## Demo

Record a walkthrough of the acceptance bar: clean start → browse → log in → successful order → trigger
the failure switch and watch compensation → blocked admin action — all through the gateway. Save the
recording here.
