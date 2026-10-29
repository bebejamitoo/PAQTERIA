import EmptyState from '../components/EmptyState';

function SummaryPage({ monitoring }) {
    const kpis = [
        { label: 'Paquetes en ruta', value: monitoring.paquetesEnRuta, detail: 'Asignados a turnos activos', tone: 'blue' },
        { label: 'Entregas exitosas', value: 0, detail: '0.0% completadas', tone: 'green' },
        { label: 'Pendientes hoy', value: monitoring.paradasPendientes, detail: 'Paradas pendientes', tone: 'orange' },
        { label: 'Incidencias abiertas', value: monitoring.incidenciasAbiertas, detail: 'Relacionadas con rutas', tone: 'red' },
    ];
    const activeRoutes = monitoring.turnos.filter((turn) => !['Cancelado', 'Finalizado'].includes(turn.estadoTurno));

    return (
        <>
            <PageHeader title="Resumen operativo" subtitle="Monitorea las rutas, capacidad y excepciones en tiempo real." status />
            <section className="content-section">
                <div className="section-heading"><h2>Operación de hoy</h2><p>Indicadores generales de la jornada actual.</p></div>
                <div className="kpi-grid">
                    {kpis.map((kpi) => <article className={`kpi-card ${kpi.tone}`} key={kpi.label}><span>{kpi.label}</span><strong>{kpi.value}</strong><small>{kpi.detail}</small></article>)}
                </div>
                <div className="split-grid summary-grid">
                    <section className="panel"><PanelTitle title="Cobertura de rutas" subtitle="Actualizada con información de operación." /><EmptyState title="Sin cobertura disponible" description="No hay posiciones de rutas para mostrar." /></section>
                    <section className="panel"><PanelTitle title="Rutas en movimiento" /><EmptyState title={activeRoutes.length === 0 ? 'No hay rutas activas' : `${activeRoutes.length} ruta(s) activa(s)`} description={activeRoutes.length === 0 ? 'Las rutas activas aparecerán aquí.' : 'La operación está actualizada.'} /></section>
                </div>
            </section>
        </>
    );
}

export function PageHeader({ title, subtitle, action, status = false }) {
    return <header className="page-header"><div><h1>{title}</h1><p>{subtitle}</p></div><div className="header-actions">{status && <span className="live-status"><i /> En vivo</span>}{action}</div></header>;
}

export function PanelTitle({ title, subtitle }) {
    return <div className="panel-title"><h2>{title}</h2>{subtitle && <p>{subtitle}</p>}</div>;
}

export default SummaryPage;
