import { useEffect, useState } from 'react';
import EmptyState from '../components/EmptyState';
import { PageHeader } from './SummaryPage';

const emptyForm = {
    nombre: '',
    ciudad: '',
    direccion: '',
};

async function readResponseBody(response) {
    const text = await response.text();
    if (!text.trim()) return null;

    try {
        return JSON.parse(text);
    } catch {
        return { message: text };
    }
}

function CentersPage() {
    const [centers, setCenters] = useState([]);
    const [form, setForm] = useState(emptyForm);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [message, setMessage] = useState(null);

    useEffect(() => {
        loadCenters();
    }, []);

    async function loadCenters() {
        setIsLoading(true);
        try {
            const response = await fetch('/api/distribution-centers');
            const body = await readResponseBody(response);
            if (!response.ok) throw new Error(body?.message ?? `No fue posible consultar los centros (${response.status}).`);
            setCenters(Array.isArray(body) ? body : []);
        } catch (error) {
            setMessage({ type: 'error', text: error.message });
        } finally {
            setIsLoading(false);
        }
    }

    function handleChange(event) {
        const { name, value } = event.target;
        setForm((current) => ({ ...current, [name]: value }));
    }

    async function handleCreate(event) {
        event.preventDefault();
        if (isSaving) return;

        setIsSaving(true);
        setMessage(null);
        try {
            const response = await fetch('/api/distribution-centers', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(form),
            });
            const body = await readResponseBody(response);
            if (!response.ok) throw new Error(body?.message ?? `No fue posible guardar el centro (${response.status}).`);
            if (!body) throw new Error('La API guardó la solicitud sin devolver el centro creado. Reinicia el servidor e inténtalo de nuevo.');

            setForm(emptyForm);
            setMessage({ type: 'success', text: `Centro ${body.nombre} guardado correctamente.` });
            await loadCenters();
        } catch (error) {
            setMessage({ type: 'error', text: error.message });
        } finally {
            setIsSaving(false);
        }
    }

    return <>
        <PageHeader
            title="Centros de distribución"
            subtitle="Los centros se usan al registrar o importar paquetes."
        />
        <section className="content-section centers-section">
            <form className="center-form" onSubmit={handleCreate}>
                <div className="center-form-heading">
                    <div><h2>Agregar centro</h2><p>Solo los Administradores pueden cambiar este catálogo.</p></div>
                </div>
                <div className="center-form-grid">
                    <label>Nombre<input name="nombre" value={form.nombre} onChange={handleChange} placeholder="Ej. Centro de Distribución Hermosillo" required /></label>
                    <label>Ciudad<input name="ciudad" value={form.ciudad} onChange={handleChange} placeholder="Ej. Hermosillo" required /></label>
                    <label>Dirección<input name="direccion" value={form.direccion} onChange={handleChange} placeholder="Ej. Blvd. Luis Encinas #123" required /></label>
                    <button className="primary-button center-save-button" disabled={isSaving} type="submit">{isSaving ? 'Guardando...' : 'Guardar centro'}</button>
                </div>
            </form>
            {message && <div className={`operation-message ${message.type}`} role="status">{message.text}</div>}
            <section className="panel centers-table-panel">
                <div className="table-wrapper"><table><thead><tr><th>Centro</th><th>Ciudad</th><th>Dirección</th></tr></thead><tbody>{centers.map((center) => <tr key={center.idCentro}><td>{center.nombre}</td><td>{center.ciudad}</td><td>{center.direccion}</td></tr>)}</tbody></table></div>
                {isLoading ? <EmptyState title="Cargando centros" description="Consultando el catálogo de distribución." /> : centers.length === 0 && <EmptyState title="Sin centros disponibles" description="Los centros registrados aparecerán en esta tabla." />}
            </section>
        </section>
    </>;
}

export default CentersPage;
