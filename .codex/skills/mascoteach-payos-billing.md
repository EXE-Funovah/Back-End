---
description: |
  Use this skill when working on Mascoteach payment, PayOS, billing, Premium upgrade, payment links,
  VietQR checkout, payment webhooks, Payment_Orders, Payment_Webhook_Events, premium_expires_at,
  subscription_tier, cancel payment, or document quota behavior tied to Premium.
---

# Mascoteach - PayOS Billing Skill

## Current payment shape

Mascoteach uses PayOS for Vietnamese VND payments. Stripe is out of scope.

Current paid plans:

- `PRO_MONTHLY`: `119000` VND, adds 30 days.
- `PRO_YEARLY`: `1188000` VND, adds 365 days.

Premium is time-based, not automatic recurring subscription.

Premium is active only when:

```text
User.SubscriptionTier == "Premium"
AND User.PremiumExpiresAt != null
AND User.PremiumExpiresAt > DateTime.UtcNow
```

Do not let the frontend directly grant Premium.

## Database shape

Payment depends on:

- `Users.premium_expires_at`
- `Payment_Orders`
- `Payment_Webhook_Events`

Important `Payment_Orders` columns:

- `user_id`
- `order_code`
- `plan_code`
- `amount`
- `status`: `Pending`, `Paid`, `Cancelled`, `Expired`, `Failed`
- `provider`: `PayOS`
- `payment_link_id`
- `checkout_url`
- `qr_code`
- `payos_reference`
- `paid_at`
- `cancelled_at`
- `updated_at`
- `is_deleted`

This project is DB-first. Do not add EF migrations unless the team explicitly changes strategy.

## Backend endpoints

Current endpoints:

- `GET /api/Billing/plans`
- `POST /api/Billing/create-payment-link`
- `GET /api/Billing/me`
- `GET /api/Billing/orders/me`
- `PATCH /api/Billing/orders/{orderCode}/cancel`
- `POST /api/Billing/payos-webhook`

All billing endpoints require `[Authorize]` except `payos-webhook`, which is `[AllowAnonymous]` but must verify PayOS signature.

## Create payment link flow

1. Frontend calls `POST /api/Billing/create-payment-link` with `planCode`.
2. Backend uses `CurrentUserId`; never trust user id from request body.
3. Backend checks for an existing reusable `Pending` order for the same user and same plan created within the last 5 minutes.
4. If reusable order exists and has `checkout_url`, backend returns that existing link and its original `expiresAt`; reusing must not restart the countdown.
5. If no reusable order exists, backend marks same-user same-plan `Pending` orders older than 5 minutes as `Expired`.
6. Backend creates a new `PaymentOrder` with `Pending` status.
7. Backend sends PayOS `expiredAt = PaymentOrder.CreatedAt + 5 minutes` as Unix seconds.
8. Backend sends one PayOS `items` entry describing the selected Mascoteach Pro plan.
9. Backend signs PayOS request with `PayOS:ChecksumKey`.
10. Backend calls PayOS create payment link API.
11. Backend stores `payment_link_id`, `checkout_url`, and `qr_code`.
12. Backend returns `checkoutUrl`, `returnUrl`, `cancelUrl`, and `expiresAt`.
13. Frontend displays a countdown from `expiresAt` and calls create-payment-link again when it reaches zero.

PayOS request signature uses fields in alphabetical order:

```text
amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}
```

Use HMAC SHA256 with `PayOS:ChecksumKey`, lowercase hex.

## Redirect vs embedded checkout

Redirect flow:

- Frontend opens `checkoutUrl` directly.
- PayOS uses the `returnUrl` and `cancelUrl` backend sent when creating the payment link.
- Frontend does not pass a separate `RETURN_URL`.

Embedded flow:

- Frontend uses PayOS embedded SDK/iframe inside its own checkout page.
- Frontend must pass the same `RETURN_URL` that backend used to create the payment link.
- Backend returns `returnUrl` in `POST /api/Billing/create-payment-link`.
- Frontend must use this returned `returnUrl` as the source of truth.

If embedded frontend passes a different `RETURN_URL` than backend used, PayOS can show:

```text
Thong tin truyen len khong hop le
```

This is a return URL mismatch, not a QR, CORS, webhook, or database issue.

## Webhook rules

PayOS calls:

```text
POST /api/Billing/payos-webhook
```

Backend must:

1. Verify `signature` against the `data` object using `PayOS:ChecksumKey`.
2. Require successful payment payload before granting Premium.
3. Find the order by `orderCode`.
4. Validate amount matches the order amount.
5. If the order is already `Paid`, do not extend Premium again.
6. Only `Pending` orders can be changed to `Paid`; ignore successful webhooks for `Expired`, `Cancelled`, or `Failed` orders.
7. Mark order `Paid`, set `paid_at`, store reference/payment link id.
8. Extend Premium in the same transaction.

Premium extension rule:

```text
baseDate = user.PremiumExpiresAt > now ? user.PremiumExpiresAt : now
user.PremiumExpiresAt = baseDate + plan.DurationDays
user.SubscriptionTier = "Premium"
```

Do not grant Premium from return URL or frontend-only data.

## Cancel payment rules

When a user presses Cancel on PayOS, PayOS redirects the browser to the frontend cancel URL and appends query params similar to:

```text
/checkout/cancel?code=00&id=...&cancel=true&status=CANCELLED&orderCode=178...
```

Frontend must read `orderCode` and call:

```text
PATCH /api/Billing/orders/{orderCode}/cancel
```

Backend must:

- Use `CurrentUserId`.
- Only cancel orders owned by the current user.
- Only change `Pending` orders to `Cancelled`.
- Never cancel `Paid` orders.
- Set `cancelled_at` and `updated_at`.

Do not expose a public unauthenticated cancel endpoint.

## Frontend URLs

Development:

- `https://dev.mascoteach.com/checkout`
- `https://dev.mascoteach.com/checkout/cancel`

Production:

- `https://mascoteach.com/checkout`
- `https://mascoteach.com/checkout/cancel`

Return/cancel pages are UI feedback only. They must refresh backend state through Billing APIs.

## PayOS channels and webhook URLs

Use separate PayOS payment channels when available:

- Dev channel webhook: `https://api-dev.mascoteach.com/api/Billing/payos-webhook`
- Production channel webhook: `https://api.mascoteach.com/api/Billing/payos-webhook`

If using a single PayOS channel, its one webhook URL must point to the environment currently receiving real payments.

## Configuration

Required config:

- `PayOS:ClientId`
- `PayOS:ApiKey`
- `PayOS:ChecksumKey`
- `PayOS:ReturnUrl`
- `PayOS:CancelUrl`
- `PayOS:BaseUrl`

Development GitHub Secrets:

- `DEV_PAYOS_CLIENT_ID`
- `DEV_PAYOS_API_KEY`
- `DEV_PAYOS_CHECKSUM_KEY`
- `DEV_PAYOS_RETURN_URL`
- `DEV_PAYOS_CANCEL_URL`

Production GitHub Secrets:

- `PAYOS_CLIENT_ID`
- `PAYOS_API_KEY`
- `PAYOS_CHECKSUM_KEY`
- `PAYOS_RETURN_URL`
- `PAYOS_CANCEL_URL`

When adding or changing PayOS config, also update `.github/workflows/auto-build-deploy-dotnet.yml`.

## Document quota integration

`DocumentService` must not treat all `SubscriptionTier == "Premium"` users as unlimited.

Expired Premium users must fall back to Freemium active document quota.

## Testing

For code changes, run:

```powershell
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj --no-restore
dotnet build EXE101-Mascoteach-Backend.sln --no-restore
```

Important test cases:

- Plan amounts are `119000` and `1188000`.
- PayOS request signature is deterministic.
- Webhook signature verification rejects tampering.
- Successful webhook marks order `Paid`.
- Successful webhook extends active Premium from existing expiry.
- Duplicate paid webhook does not extend Premium twice.
- Amount mismatch does not grant Premium.
- Repeated create-payment-link calls for the same user and plan within 5 minutes reuse the existing pending checkout URL.
- Reused links return the original `expiresAt` instead of restarting the five-minute countdown.
- New PayOS requests include `expiredAt` and selected-plan `items`.
- Pending links older than five minutes are marked `Expired` when the frontend requests a replacement link.
- Successful webhooks for non-pending orders do not grant Premium.
- Owner can cancel a pending order.
- Paid order cannot be cancelled.
- Other user's order cannot be cancelled.
- Expired Premium uses Freemium document quota.

## Common mistakes

- Do not use `POST` for cancel; use `PATCH /api/Billing/orders/{orderCode}/cancel`.
- Do not trust PayOS return URL to grant Premium.
- Do not expose cancel without JWT.
- Do not forget webhook signature verification.
- Do not leave production PayOS channel pointing to `api-dev`.
- Do not forget PayOS GitHub Secrets before deploy.
- Do not hardcode PayOS secrets in committed config.
- Do not create a new PayOS payment link on every plan toggle when a same-plan pending link is still reusable.
- Do not calculate a fresh `expiresAt` when returning a reused link; always derive it from the order's original `created_at`.
