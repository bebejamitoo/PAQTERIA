import { useState } from 'react';
import EmptyState from '../components/EmptyState';
import { PageHeader } from './SummaryPage';

function DriversPage() {
    const [drivers] = useState([]);
    return <><PageHeader title="Repartidores" subtitle="Consulta el estado operativo de tu equipo." action={<button className="primary-button" type="button">+ Registrar repartidor</button>} /><section className="content-section"><div className="status-filters"><span>Todos (0)</span><span>En ruta (0)</span><span>En descanso (0)</span><span>Disponibles (0)</span></div><section className="panel"><div className="driver-grid">{drivers.map((driver) => <article key={driver.id}>{driver.name}</article>)}</div>{drivers.length === 0 && <EmptyState title="Sin repartidores registrados" description="Los repartidores aparecerán aquí cuando se conecte la información operativa." />}</section></section></>;
}
export default DriversPage;
