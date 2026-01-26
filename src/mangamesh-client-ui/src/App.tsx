import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import Dashboard from './pages/Dashboard';
import Subscriptions from './pages/Subscriptions';
import ImportChapter from './pages/ImportChapter';
import Storage from './pages/Storage';
import SeriesDetails from './pages/SeriesDetails';
import Reader from './components/Reader';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Layout />}>
          <Route index element={<Dashboard />} />
          <Route path="subscriptions" element={<Subscriptions />} />
          <Route path="series/:seriesId" element={<SeriesDetails />} />
          <Route path="read/:manifestHash" element={<Reader />} />
          <Route path="import" element={<ImportChapter />} />
          <Route path="storage" element={<Storage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
