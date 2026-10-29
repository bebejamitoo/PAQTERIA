import { useState } from 'react';
import EmptyState from '../components/EmptyState';
import { PageHeader, PanelTitle } from './SummaryPage';

function ReportsPage() {
    const [reportSeries] = useState([]);
    const [effectiveness] = useState(0);
    return <><PageHeader title="Corte del día y reportes" subtitle="Consulta el desempeño y la evolución de la operación." action={<button className="secondary-button" type="button">Exportar reporte</button>} /><section className="content-section"><div className="kpi-grid report-kpis"><article className="kpi-card blue"><span>Paquetes procesados</span><strong>0</strong><small>Durante la jornada</small></article><article className="kpi-card green"><span>Efectividad</span><strong>{effectiveness.toFixed(1)}%</strong><small>Entregas exitosas</small></article><article className="kpi-card orange"><span>Rutas completadas</span><strong>0</strong><small>Sin datos de rutas</small></article></div><div className="split-grid reports-grid"><section className="panel"><PanelTitle title="Evolución de entregas" subtitle="Últimos periodos disponibles" />{reportSeries.length === 0 && <EmptyState title="Sin evolución disponible" description="La gráfica se generará cuando existan reportes." />}</section><section className="panel"><PanelTitle title="Resumen de desempeño" />{reportSeries.length === 0 && <EmptyState title="Sin resumen disponible" description="No hay datos de desempeño para mostrar." />}</section></div></section></>;
}
export default ReportsPage;
