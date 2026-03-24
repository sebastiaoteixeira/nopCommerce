#!/usr/bin/env python3
"""
Generate OpenTelemetry Component Diagram for nopCommerce.

This script creates a UML-style component diagram showing how OpenTelemetry
instrumentation components are integrated into the nopCommerce application
and how they connect to the observability backend.
"""

import subprocess
from pathlib import Path
from typing import Final

DOT_BINARY: Final[str] = "/usr/bin/dot"
OUTPUT_PATH: Final[Path] = Path(__file__).parent / "component-diagram.png"

DOT_SOURCE: Final[str] = '''
digraph nopCommerce_OTel {
    graph [rankdir=LR, splines=true, nodesep=0.5, ranksep=0.9, dpi=150,
           fontname="Arial", pad="0.4", compound=true];
    node [shape=component, style=filled, fontname="Arial", fontsize=10,
          width=1.6, height=0.6];
    edge [fontname="Arial", fontsize=8];

    // ── Entry ──
    browser [label="Browser", fillcolor="#E8F4F8", style="filled,rounded"];

    // ── Presentation Layer ──
    subgraph cluster_presentation {
        label="Nop.Web (Presentation)"; style=filled; color="#F5F5F5"; fontsize=10;
        tracingFilter [label="TracingActionFilter\\n(Global AOP Filter)", fillcolor="#C8E6C9"];
        catalogController [label="CatalogController", fillcolor="#E8F4F8"];
        productController [label="ProductController", fillcolor="#E8F4F8"];
        catalogFactory [label="CatalogModelFactory", fillcolor="#E8F4F8"];
    }

    // ── Instrumentation Layer ──
    subgraph cluster_framework {
        label="Nop.Web.Framework (Instrumentation Layer)";
        style=filled; color="#E8F5E9"; fontsize=10;

        subgraph cluster_decorators {
            label="Decorators"; style=dashed; color="#A5D6A7";
            instrumentedService [label="Instrumented\\nProductService", fillcolor="#C8E6C9"];
            instrumentedPricing [label="Instrumented\\nPriceCalculation\\nService", fillcolor="#C8E6C9"];
            instrumentedRepo [label="Instrumented\\nProductRepository", fillcolor="#C8E6C9"];
        }

        subgraph cluster_events {
            label="Event-Driven Telemetry"; style=dashed; color="#FFE082";
            pageViewEvent [label="ProductDetailPage\\nViewedEvent", fillcolor="#FFF9C4"];
            pageViewConsumer [label="ProductPageView\\nTelemetryConsumer", fillcolor="#FFF9C4"];
        }

        nopTelemetry [label="NopTelemetry\\n(ActivitySource + Meter)", fillcolor="#A5D6A7"];
        otelStartup [label="OpenTelemetryStartup\\n(DI Wiring)", fillcolor="#C8E6C9"];
    }

    // ── Business Logic ──
    subgraph cluster_services {
        label="Nop.Services (Business Logic)"; style=filled; color="#F5F5F5"; fontsize=10;
        productService [label="ProductService", fillcolor="#E8F4F8"];
        priceService [label="PriceCalculation\\nService", fillcolor="#E8F4F8"];
        eventPublisher [label="IEventPublisher\\n(nopCommerce built-in)", fillcolor="#E8F4F8"];
    }

    // ── Data Layer ──
    subgraph cluster_data {
        label="Nop.Data"; style=filled; color="#F5F5F5"; fontsize=10;
        repository [label="IRepository<T>\\n(EntityRepository)", fillcolor="#E8F4F8"];
        database [label="SQL Server", fillcolor="#E8F4F8", shape=cylinder];
    }

    // ── Observability Backend ──
    subgraph cluster_obs {
        label="Observability Backend"; style=filled; color="#E0E0E0";
        fontsize=10;
        otelCollector [label="OTel Collector\\n(PII Sanitization)", fillcolor="#D3D3D3"];
        jaeger [label="Jaeger", fillcolor="#D3D3D3"];
        prometheus [label="Prometheus", fillcolor="#D3D3D3"];
        grafana [label="Grafana", fillcolor="#D3D3D3"];
    }

    // ════════ REQUEST FLOW (black) ════════
    browser -> tracingFilter [label="HTTP"];
    tracingFilter -> catalogController [label="invoke"];
    tracingFilter -> productController [label="invoke"];
    catalogController -> catalogFactory;
    productController -> catalogFactory;
    catalogFactory -> instrumentedService [label="IProductService"];
    catalogFactory -> instrumentedPricing [label="IPriceCalculation\\nService"];
    instrumentedService -> productService [label="base.Search\\nProductsAsync()"];
    instrumentedPricing -> priceService [label="base.GetFinal\\nPriceAsync()"];
    productService -> instrumentedRepo [label="IRepository<Product>"];
    instrumentedRepo -> repository [label="delegates"];
    repository -> database [label="SQL"];

    // ════════ EVENT FLOW (orange) ════════
    tracingFilter -> eventPublisher [label="PublishAsync\\n(on ProductDetails)", style=bold, color="#E65100"];
    eventPublisher -> pageViewEvent [label="dispatches", style=bold, color="#E65100"];
    pageViewEvent -> pageViewConsumer [label="IConsumer<T>\\nauto-discovered", style=bold, color="#E65100"];

    // ════════ TELEMETRY (green) ════════
    tracingFilter -> nopTelemetry [label="spans", style=bold, color="#2E7D32"];
    instrumentedService -> nopTelemetry [label="spans + metrics", style=bold, color="#2E7D32"];
    instrumentedPricing -> nopTelemetry [label="spans + metrics", style=bold, color="#2E7D32"];
    instrumentedRepo -> nopTelemetry [label="spans + metrics", style=bold, color="#2E7D32"];
    pageViewConsumer -> nopTelemetry [label="metric", style=bold, color="#2E7D32"];

    // ════════ EXPORT (blue) ════════
    nopTelemetry -> otelCollector [label="OTLP/gRPC", style=bold, color="#1565C0"];

    // ════════ BACKEND ════════
    otelCollector -> jaeger [label="traces"];
    otelCollector -> prometheus [label="metrics"];
    jaeger -> grafana [label="query"];
    prometheus -> grafana [label="query"];

    // ════════ DI WIRING (dashed) ════════
    otelStartup -> tracingFilter [style=dashed, color="#999999", constraint=false];
    otelStartup -> instrumentedService [style=dashed, color="#999999", constraint=false];
    otelStartup -> instrumentedPricing [style=dashed, color="#999999", constraint=false];
    otelStartup -> instrumentedRepo [style=dashed, color="#999999", constraint=false];
}
'''


def generate_diagram() -> None:
    """Generate the component diagram PNG using graphviz dot binary."""
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)

    result = subprocess.run(
        [DOT_BINARY, "-Tpng", "-o", str(OUTPUT_PATH)],
        input=DOT_SOURCE.encode("utf-8"),
        capture_output=True,
        check=True
    )

    if result.returncode == 0:
        print(f"Component diagram generated successfully: {OUTPUT_PATH.absolute()}")
    else:
        raise RuntimeError(f"Failed to generate diagram: {result.stderr.decode()}")


if __name__ == "__main__":
    generate_diagram()
