# Cronograma: Plataforma Netflix Analytics

- **Inicio:** martes 3 de noviembre de 2026
- **Entrega:** viernes 27 de noviembre de 2026
- **Jornadas:** días hábiles (lunes a viernes), 4 semanas

Las tareas detalladas de cada barra están en `tasks.md`; los números de grupo se indican entre corchetes.

## Diagrama de Gantt

```mermaid
gantt
    title Netflix Analytics · 3 al 27 de noviembre de 2026
    dateFormat YYYY-MM-DD
    axisFormat %d %b
    excludes weekends
    todayMarker off

    section Equipo completo
    Kickoff y definición de arquitectura        :done, t1, 2026-11-03, 2d
    Integración y pruebas [6]                   :t2, 2026-11-23, 2d
    Guion de demo, README y ensayo [6]          :t3, 2026-11-25, 2d
    Entrega                                     :milestone, crit, m1, 2026-11-27, 0d

    section Diana Paola Montes · Datos y ETL
    Análisis de datasets                        :d1, 2026-11-05, 2d
    Docker Compose y schema CQL [1]             :d2, 2026-11-09, 2d
    ETL limpieza y reparto [2]                  :d3, 2026-11-11, 3d
    Agregados [2]                               :d4, 2026-11-16, 2d

    section Jonathan Cervantes · Mensajería
    Colas y contratos de mensajes               :j1, 2026-11-05, 2d
    Publisher Python con confirms [2]           :j2, 2026-11-09, 2d
    Consumidores y escritura en Cassandra [3]   :crit, j3, 2026-11-11, 5d
    Botones de demo, DLQ y duplicados [5]       :j4, 2026-11-18, 3d

    section Fernanda Baez · Backend .NET
    App .NET y conexión a Cassandra [3]         :f1, 2026-11-05, 2d
    Servicios de consulta [4]                   :f2, 2026-11-09, 5d
    Caché del catálogo [4]                      :f3, 2026-11-16, 2d
    LiveEvents y poller de colas [5]            :f4, 2026-11-18, 3d

    section Ximena Jasso · Frontend Blazor
    Mockups y tema visual                       :x1, 2026-11-05, 2d
    Layout y vista Resumen [4]                  :x2, 2026-11-09, 5d
    Catálogo, Ranking y Personas [4]            :x3, 2026-11-16, 5d
    Vista Mensajería [5]                        :crit, x4, 2026-11-23, 2d
```

## Hitos

| Fecha | Hito | Criterio de cumplimiento |
|---|---|---|
| 04-nov | Arquitectura definida | Change OpenSpec con propuesta, diseño, specs y tareas |
| 10-nov | Infraestructura lista | `docker compose up` con Cassandra y RabbitMQ healthy y esquema aplicado |
| 13-nov | Primer flujo de punta a punta | Un título viaja ETL → RabbitMQ → consumidor → Cassandra |
| 17-nov | Carga completa | Catálogo y agregados en Cassandra, sin duplicados al recargar |
| 20-nov | Vistas de datos listas | Resumen, Catálogo, Ranking y Personas funcionando |
| 24-nov | Dashboard completo | 5 vistas, incluida Mensajería con sus 2 botones de demo |
| 26-nov | Demo ensayada | Guion ≤ 10 min ejecutado sin fallas |
| 27-nov | **Entrega** | Repositorio, documentación y demo en vivo |

## Ruta crítica

**Consumidores y escritura en Cassandra → Vista Mensajería → Integración.** Sin consumidores no hay datos ni eventos, y la vista Mensajería es la pieza central de la demo. Por eso esas barras están marcadas como `crit`.
